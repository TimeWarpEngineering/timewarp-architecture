#region Purpose
// Whether Microsoft 365 (Entra) sign-in is offered right now: configuration scheme gate AND the site settings policy.
#endregion

#region Design
// One copy of the "offered" rule, read by GetEntraSignInOffered (the login page's public boolean) and
// GetCredentials (which offers Link Microsoft 365, task 279). options.Enabled is the scheme-registration
// gate; settings.EntraSignInEnabled is the runtime offer. Both must be true or the challenge would 404
// (no scheme) or 403 (policy). The registered store seeds on first read (SeedOnReadSiteSettingsStore,
// task 254), so an emptied table is re-seeded from configuration; a null read (table not migrated) is
// not offered. A disabled scheme short-circuits before the store is read.
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
    if (!options.Enabled)
    {
      return false;
    }

    SiteSettings? settings = await siteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
    return settings is { EntraSignInEnabled: true };
  }
}
