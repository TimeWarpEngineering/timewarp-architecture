#region Purpose
// Thread-safe in-memory ISiteSettingsStore for zero-infra hosts and store-contract tests.
#endregion

#region Design
// Same CAS mechanics as InMemoryPrincipalStore: WriteLock around Add/Update because Entity equality
// is type+Id and would make ConcurrentDictionary.TryUpdate always succeed. Get is lock-free and
// returns Snapshot. Add rejects a second singleton and a non-SingletonId. Update rejects missing
// row, non-singleton id, and version mismatch (store untouched). Host EF (EfSiteSettingsStore)
// must match these semantics.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Process-local <see cref="ISiteSettingsStore"/> for tests and zero-infra hosts.
/// </summary>
public sealed class InMemorySiteSettingsStore : ISiteSettingsStore
{
  private readonly Lock WriteLock = new();
  private SiteSettings? Stored;

  /// <inheritdoc />
  public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    SiteSettings? stored = Stored;
    return Task.FromResult(stored?.Snapshot(stored.Version));
  }

  /// <inheritdoc />
  public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(siteSettings);
    cancellationToken.ThrowIfCancellationRequested();
    EnsureSingleton(siteSettings);

    lock (WriteLock)
    {
      if (Stored is not null)
      {
        throw new InvalidOperationException("Site settings already exist.");
      }

      Stored = siteSettings.Snapshot(siteSettings.Version);
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(siteSettings);
    cancellationToken.ThrowIfCancellationRequested();
    EnsureSingleton(siteSettings);

    lock (WriteLock)
    {
      if (Stored is null)
      {
        throw new InvalidOperationException("Site settings do not exist.");
      }

      if (siteSettings.Version != Stored.Version)
      {
        throw new ConcurrencyConflictException(
          typeof(SiteSettings),
          siteSettings.Id.ToString(),
          siteSettings.Version,
          Stored.Version);
      }

      Stored = siteSettings.Snapshot(EntityVersion.Next(Stored.Version));
    }

    return Task.CompletedTask;
  }

  private static void EnsureSingleton(SiteSettings siteSettings)
  {
    if (siteSettings.Id != SiteSettings.SingletonId)
    {
      throw new InvalidOperationException(
        $"Site settings id must be the singleton '{SiteSettings.SingletonId}'.");
    }
  }
}
