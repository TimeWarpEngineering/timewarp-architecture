#region Purpose
// ISiteSettingsStore decorator: a read that finds the store empty seeds it, so every reader gets a row.
#endregion

#region Design
// Task 254: the single seed seam is the store's own read, not a helper each reader must remember —
// a new reader that takes ISiteSettingsStore cannot bypass it, and the Settings handlers keep their
// ISiteSettingsStore dependency (no TWA0009 edge into Identity.Application). An empty store is
// treated exactly like a first boot: SiteSettingsSeeder.GetOrSeedAsync(IsDevelopment) with the
// same IsDevelopment the hosted service passes (IHostEnvironment.IsDevelopment(), read when the
// scoped decorator is built). That recovers any path that empties the table while the host runs (`dev db reset`
// re-migrates, a manual DELETE) without a restart. Concurrent empty reads race on AddAsync; the
// seeder's re-Get on InvalidOperationException leaves one row. Postgres 42P01 (identity.site_settings
// not migrated yet) is not retried per request — the read returns null and logs a warning, so
// readers take their fail-closed path (Settings 503 "unavailable", sign-in not offered/refused)
// instead of a 500; the boot-time hosted service keeps its bounded retry. Add/Update pass through.
// Registered by SiteSettingsSeedRegistration over the keyed InnerStoreKey store.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Identity;

public sealed class SeedOnReadSiteSettingsStore : ISiteSettingsStore
{
  /// <summary>DI key of the undecorated store the decorator and the seeder wrap.</summary>
  public const string InnerStoreKey = "site-settings-inner-store";

  private static readonly Action<ILogger, Exception?> LogUndefinedTable =
    LoggerMessage.Define
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogUndefinedTable)),
      "Site settings read found identity.site_settings undefined; web-migrations has not applied yet. Readers see site settings as unavailable until it does."
    );

  private readonly ISiteSettingsStore Inner;
  private readonly SiteSettingsSeeder Seeder;
  private readonly bool IsDevelopment;
  private readonly ILogger<SeedOnReadSiteSettingsStore> Logger;

  public SeedOnReadSiteSettingsStore(
    ISiteSettingsStore inner,
    SiteSettingsSeeder seeder,
    bool isDevelopment,
    ILogger<SeedOnReadSiteSettingsStore> logger)
  {
    Inner = inner;
    Seeder = seeder;
    IsDevelopment = isDevelopment;
    Logger = logger;
  }

  public async Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      SiteSettings? existing = await Inner.GetAsync(cancellationToken).ConfigureAwait(false);
      if (existing is not null)
      {
        return existing;
      }

      return await Seeder.GetOrSeedAsync(IsDevelopment, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (SiteSettingsSeeder.IsUndefinedTable(exception))
    {
      LogUndefinedTable(Logger, exception);
      return null;
    }
  }

  public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
    Inner.AddAsync(siteSettings, cancellationToken);

  public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
    Inner.UpdateAsync(siteSettings, cancellationToken);
}
