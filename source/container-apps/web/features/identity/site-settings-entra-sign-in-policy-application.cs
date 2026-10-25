#region Purpose
// Default IEntraSignInPolicy: site-settings offer/bootstrap plus admin tenant acceptance.
#endregion

#region Design
// The registered store seeds on first read (SeedOnReadSiteSettingsStore, task 254), so an emptied
// table evaluates against the configuration-seeded policy. A null read (table not migrated, or a
// host wired without the decorator) is treated as disabled (Sign-in disabled on Challenge;
// untrusted on ticket modes) so it cannot fail-open. Challenge checks EntraSignInEnabled only.
// SyncHit, Link, and BootstrapCreate call SiteSettings.AcceptsEntraTenant: Restricted requires the
// token tid against EntraAcceptedTenants (the domain is display-only). Any accepts any non-empty
// tid. Authentication:Entra:TenantId
// is the OIDC authority, not this check. BootstrapCreate also requires AllowBootstrap.
// Untrusted title stays "Untrusted tenant"; bootstrap title stays "Bootstrap not allowed"
// (219-002 tests). Issuer pinning stays in EntraIssuerValidator.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Identity;

public sealed class SiteSettingsEntraSignInPolicy : IEntraSignInPolicy
{
  private readonly ISiteSettingsStore SiteSettingsStore;

  public SiteSettingsEntraSignInPolicy(ISiteSettingsStore siteSettingsStore)
  {
    SiteSettingsStore = siteSettingsStore;
  }

  public async Task<EntraSignInDecision> EvaluateAsync(
    EntraSignInMode mode,
    Guid? tenantId,
    CancellationToken cancellationToken = default)
  {
    SiteSettings? settings = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);

    if (mode == EntraSignInMode.Challenge)
    {
      if (settings is not { EntraSignInEnabled: true })
      {
        return EntraSignInDecision.Refuse(IdentityProblems.SignInDisabled());
      }

      return EntraSignInDecision.Allow();
    }

    if (settings is null || tenantId is null || !settings.AcceptsEntraTenant(tenantId.Value))
    {
      return EntraSignInDecision.Refuse(IdentityProblems.UntrustedTenant());
    }

    if (mode == EntraSignInMode.BootstrapCreate && !settings.EntraAllowBootstrap)
    {
      return EntraSignInDecision.Refuse(IdentityProblems.BootstrapNotAllowed());
    }

    return EntraSignInDecision.Allow();
  }
}
