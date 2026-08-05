#region Purpose
// EF Core IProfileStore: durable Profile persistence behind the postgres flag.
#endregion

#region Design
// Task 148 D4: mirrors EfPrincipalStore dual-mode — host-owned, application stays free of
// PostgresDbContext. Semantics match InMemoryProfileStore:
//   - Find: AsNoTracking row or null
//   - Add: insert; unique PK (23505) → InvalidOperationException so create-if-missing re-finds
// Scoped lifetime: depends on scoped PostgresDbContext. InMemoryProfileStoresModule registers
// singleton InMemoryProfileStore; PostgresDbModule replaces when connected.
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
