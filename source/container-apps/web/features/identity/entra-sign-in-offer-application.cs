#region Purpose
// Whether Microsoft 365 (Entra) sign-in is offered right now: configuration scheme gate AND the site settings policy.
#endregion

#region Design
// One copy of the "offered" rule, read by GetEntraSignInOffered and GetCredentials (whose
// CanLinkMicrosoft365 flag uses it, task 282). Microsoft365 must be listed on SignInOptions, and
// settings.EntraSignInEnabled must be on. Both must be true or the challenge would 404 (no scheme)
// or 403 (policy). The registered store seeds on first read (SeedOnReadSiteSettingsStore, task 254),
// so an emptied table is re-seeded from configuration; a null read (table not migrated) is not
// offered. The store is read even when Microsoft365 is not listed — the read is what re-seeds an
// emptied table (site-settings-seed-on-read-tests pins it), so do not skip the store read.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Identity;

public static class EntraSignInOffer
{
  /// <summary>True when Microsoft365 is listed and site settings allow Entra sign-in.</summary>
  public static async Task<bool> IsOfferedAsync
  (
    SignInOptions signInOptions,
    ISiteSettingsStore siteSettingsStore,
    CancellationToken cancellationToken
  )
  {
    SiteSettings? settings = await siteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
    return signInOptions.Lists(SignInMethod.Microsoft365) && settings is { EntraSignInEnabled: true };
  }
}
