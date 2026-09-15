#region Purpose
// EF Core ISiteSettingsStore: durable singleton site-settings behind the postgres flag.
#endregion

#region Design
// Host-owned; TimeWarp.Identity stays EF-free. Semantics match InMemorySiteSettingsStore:
// snapshot-on-get, Add persists Version as-is, Update CAS via Version then Snapshot(Next).
// DbUpdateConcurrencyException translates to ConcurrencyConflictException. Reads AsNoTracking.
// Scoped lifetime (PostgresDbContext). InMemoryIdentityStoresModule still registers the
// in-memory singleton; PostgresDbModule replaces this type when a connection string is present.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Infrastructure;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Foundation.Entities;
using TimeWarp.Identity;

public sealed class EfSiteSettingsStore : ISiteSettingsStore
{
  private readonly PostgresDbContext Db;

  public EfSiteSettingsStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    SiteSettings? stored = await Db.SiteSettings.AsNoTracking()
      .FirstOrDefaultAsync(settings => settings.Id == SiteSettings.SingletonId, cancellationToken)
      .ConfigureAwait(false);
    return stored?.Snapshot(stored.Version);
  }

  public async Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(siteSettings);
    cancellationToken.ThrowIfCancellationRequested();
    EnsureSingleton(siteSettings);

    SiteSettings stored = siteSettings.Snapshot(siteSettings.Version);
    bool exists = await Db.SiteSettings.AsNoTracking()
      .AnyAsync(row => row.Id == siteSettings.Id, cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      throw new InvalidOperationException("Site settings already exist.");
    }

    Db.SiteSettings.Add(stored);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      Db.Entry(stored).State = EntityState.Detached;
      throw new InvalidOperationException("Site settings already exist.", exception);
    }
  }

  public async Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(siteSettings);
    cancellationToken.ThrowIfCancellationRequested();
    EnsureSingleton(siteSettings);

    SiteSettings? stored = await Db.SiteSettings
      .FirstOrDefaultAsync(row => row.Id == siteSettings.Id, cancellationToken)
      .ConfigureAwait(false);
    if (stored is null)
    {
      throw new InvalidOperationException("Site settings do not exist.");
    }

    if (siteSettings.Version != stored.Version)
    {
      Db.Entry(stored).State = EntityState.Detached;
      throw new ConcurrencyConflictException(
        typeof(SiteSettings),
        siteSettings.Id.ToString(),
        siteSettings.Version,
        stored.Version);
    }

    long storedVersion = stored.Version;
    SiteSettings next = siteSettings.Snapshot(EntityVersion.Next(storedVersion));
    Db.Entry(stored).State = EntityState.Detached;
    Db.SiteSettings.Attach(next);
    EntityEntry<SiteSettings> entry = Db.Entry(next);
    entry.State = EntityState.Modified;
    entry.Property(settings => settings.Version).OriginalValue = storedVersion;
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      EntityEntry tracked = Db.Entry(next);
      if (tracked.State != EntityState.Detached)
      {
        tracked.State = EntityState.Detached;
      }

      _ = exception;
      throw new ConcurrencyConflictException(
        typeof(SiteSettings),
        siteSettings.Id.ToString(),
        storedVersion,
        EntityVersion.Next(storedVersion));
    }
  }

  private static void EnsureSingleton(SiteSettings siteSettings)
  {
    if (siteSettings.Id != SiteSettings.SingletonId)
    {
      throw new InvalidOperationException(
        $"Site settings id must be the singleton '{SiteSettings.SingletonId}'.");
    }
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
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
