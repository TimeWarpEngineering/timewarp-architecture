#region Purpose
// Default IEntraSignInPolicy: reads the site-settings singleton for Entra offered/bootstrap/tenants.
#endregion

#region Design
// Empty store is treated as disabled (Sign-in disabled on Challenge; untrusted on ticket modes)
// so a missed seed cannot fail-open. Challenge checks EntraSignInEnabled only. SyncHit checks
// trusted tenant. BootstrapCreate checks trusted tenant then AllowBootstrap. Untrusted title
// stays "Untrusted tenant"; bootstrap title stays "Bootstrap not allowed" (219-002 tests).
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

    if (settings is null || tenantId is null || !settings.IsTrustedTenant(tenantId.Value))
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
