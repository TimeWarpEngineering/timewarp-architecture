#region Purpose
// Default IEntraSignInPolicy: site-settings offered/bootstrap plus configured-tenant trust pin.
#endregion

#region Design
// Empty store is treated as disabled (Sign-in disabled on Challenge; untrusted on ticket modes)
// so a missed seed cannot fail-open. Challenge checks EntraSignInEnabled only. SyncHit and Link
// check token tid GUID-equals Authentication:Entra:TenantId. BootstrapCreate checks that pin
// then AllowBootstrap. Non-GUID TenantId (organizations / common) never matches — Untrusted
// tenant. Untrusted title stays "Untrusted tenant"; bootstrap title stays "Bootstrap not allowed"
// (219-002 tests). RFC 219 pin-the-tenant is this comparison, in this type.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Identity;

public sealed class SiteSettingsEntraSignInPolicy : IEntraSignInPolicy
{
  private readonly ISiteSettingsStore SiteSettingsStore;
  private readonly IOptions<EntraAuthenticationOptions> Options;

  public SiteSettingsEntraSignInPolicy(
    ISiteSettingsStore siteSettingsStore,
    IOptions<EntraAuthenticationOptions> options)
  {
    SiteSettingsStore = siteSettingsStore;
    Options = options;
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

    if (settings is null || tenantId is null || !MatchesConfiguredTenant(tenantId.Value))
    {
      return EntraSignInDecision.Refuse(IdentityProblems.UntrustedTenant());
    }

    if (mode == EntraSignInMode.BootstrapCreate && !settings.EntraAllowBootstrap)
    {
      return EntraSignInDecision.Refuse(IdentityProblems.BootstrapNotAllowed());
    }

    return EntraSignInDecision.Allow();
  }

  private bool MatchesConfiguredTenant(Guid tenantId)
  {
    if (tenantId == Guid.Empty)
    {
      return false;
    }

    if (!Guid.TryParse(Options.Value.TenantId, out Guid configured) || configured == Guid.Empty)
    {
      return false;
    }

    return configured == tenantId;
  }
}
