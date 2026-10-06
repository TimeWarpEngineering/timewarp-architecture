#region Purpose
// Whether Microsoft 365 (Entra) sign-in is offered right now: configuration scheme gate AND the site settings policy.
#endregion

#region Design
// One copy of the "offered" rule, read by GetEntraSignInOffered (the login page's public boolean) and
// GetCredentials (whose CanLinkMicrosoft365 flag uses it, task 282). options.Enabled is the scheme-registration
// gate; settings.EntraSignInEnabled is the runtime offer. Both must be true or the challenge would 404
// (no scheme) or 403 (policy). The registered store seeds on first read (SeedOnReadSiteSettingsStore,
// task 254), so an emptied table is re-seeded from configuration; a null read (table not migrated) is
// not offered. The store is read even when the scheme is off — the read is what re-seeds an emptied
// table (site-settings-seed-on-read-tests pins it), so do not short-circuit on Enabled.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Identity;

public static class EntraSignInOffer
{
  /// <summary>True when the Entra scheme is registered and site settings allow Entra sign-in.</summary>
  public static async Task<bool> IsOfferedAsync
  (
    EntraAuthenticationOptions options,
    ISiteSettingsStore siteSettingsStore,
    CancellationToken cancellationToken
  )
  {
    SiteSettings? settings = await siteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
    return options.Enabled && settings is { EntraSignInEnabled: true };
  }
}
