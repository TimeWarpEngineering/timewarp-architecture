#region Purpose
// Asks whether the site-settings table exists yet, without a query that fails (and logs Error) when it does not.
#endregion

#region Design
// Task 270: on a first run against an empty database web-server can boot before web-migrations
// has created identity.site_settings (the AppHost has no wait edge — see its Design region). A
// plain EF read there fails with 42P01, and EF logs that failure at Error level before any caller
// can catch it. SiteSettingsSeedHostedService asks this port first and waits while the table is
// missing, so the failing query never runs. Optional: only a store backend that can be
// unmigrated registers one (PostgresDbModule); with none registered the seed reads straight away.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public interface ISiteSettingsTableProbe
{
  /// <summary>True when the table behind ISiteSettingsStore exists; never throws for a missing table.</summary>
  Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
}
