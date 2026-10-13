#region Purpose
// EF Core IProfileStore: durable Profile persistence behind the postgres flag.
#endregion

#region Design
// Task 148 D4: mirrors EfPrincipalStore dual-mode — host-owned, application stays free of
// PostgresDbContext. Semantics match InMemoryProfileStore:
//   - Find: AsNoTracking row or null
//   - Add: insert; unique PK (23505) → InvalidOperationException so create-if-missing re-finds
//   - Update: load tracked row, copy named-mutation state, SaveChanges (aggregate Version bump)
// Scoped lifetime: depends on scoped PostgresDbContext. InMemoryProfileStoresModule registers
// singleton InMemoryProfileStore; PostgresDbModule replaces when connected.
// Task 328: FindMany is AsNoTracking WHERE Id IN (...). Search is a lower-cased contains on
// DisplayName or Email (term via ToLowerInvariant, columns via Postgres lower(), which follows the
// database LC_CTYPE; equal to InMemoryProfileStore's OrdinalIgnoreCase for ASCII). No index: a leading-wildcard LIKE cannot use a btree index, the admin
// list is small, and a pg_trgm GIN index is the upgrade path if it grows.
// Profile is IAggregateRoot — AggregateDbContext owns Version + DomainInvariantsGuard on SaveChanges.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.Profiles.Application;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed Profile store.</summary>
public sealed class EfProfileStore : IProfileStore
{
  private readonly PostgresDbContext Db;

  public EfProfileStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<Profile?> FindAsync(ProfileId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.Profiles.AsNoTracking()
      .FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Profile>> FindManyAsync(
    IReadOnlyCollection<ProfileId> ids,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(ids);
    cancellationToken.ThrowIfCancellationRequested();
    ProfileId[] wanted = [.. ids.Distinct()];
    if (wanted.Length == 0)
    {
      return [];
    }

    return await Db.Profiles.AsNoTracking()
      .Where(profile => wanted.Contains(profile.Id))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Profile>> SearchAsync(string term, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(term);
    cancellationToken.ThrowIfCancellationRequested();
    string lowered = term.ToLowerInvariant();
    // CA1862 wants a StringComparison overload, which EF cannot translate to SQL; lower-cased
    // contains is the translatable form (lower(x) LIKE '%term%').
#pragma warning disable CA1862
    return await Db.Profiles.AsNoTracking()
      .Where(profile => profile.DisplayName.ToLower().Contains(lowered)
        || (profile.Email != null && profile.Email.ToLower().Contains(lowered)))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
#pragma warning restore CA1862
  }

  public async Task AddAsync(Profile profile, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(profile);
    cancellationToken.ThrowIfCancellationRequested();

    bool exists = await Db.Profiles.AsNoTracking()
      .AnyAsync(row => row.Id == profile.Id, cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      throw new InvalidOperationException($"Profile '{profile.Id}' already exists.");
    }

    Db.Profiles.Add(profile);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      Db.Entry(profile).State = EntityState.Detached;
      throw new InvalidOperationException($"Profile '{profile.Id}' already exists.", exception);
    }
  }

  public async Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(profile);
    cancellationToken.ThrowIfCancellationRequested();

    Profile? stored = await Db.Profiles
      .FirstOrDefaultAsync(row => row.Id == profile.Id, cancellationToken)
      .ConfigureAwait(false);
    if (stored is null)
    {
      throw new InvalidOperationException($"Profile '{profile.Id}' does not exist.");
    }

    stored.Rename(profile.DisplayName);
    stored.SetEmail(profile.Email);
    stored.SetLanguage(profile.Language);
    stored.SetRegion(profile.Region);
    stored.SetTheme(profile.Theme);
    if (profile.Notifications)
    {
      stored.EnableNotifications();
    }
    else
    {
      stored.DisableNotifications();
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    // Npgsql: 23505 unique_violation. String-based so this file does not hard-depend on Npgsql types.
    Exception? current = exception;
    while (current is not null)
    {
      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("23505", StringComparison.Ordinal)
          || text.Contains("unique", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      current = current.InnerException;
    }

    return false;
  }
}
