#region Purpose
// Application-layer seam for whether Entra sign-in / bootstrap / sync-hit is allowed at runtime.
#endregion

#region Design
// Task 219-006: products (crunchit 008-004) replace this in DI without touching the named entra
// scheme. Default implementation reads ISiteSettingsStore — not EntraAuthenticationOptions —
// for EntraSignInEnabled, AllowBootstrap, and TrustedTenants. Configuration still owns scheme
// registration (Enabled) and secrets. EvaluateAsync is the only method; keep it small.
// tenantId is omitted on Challenge (OIDC has not run yet). SyncHit/BootstrapCreate pass tid.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public interface IEntraSignInPolicy
{
  Task<EntraSignInDecision> EvaluateAsync(
    EntraSignInMode mode,
    Guid? tenantId,
    CancellationToken cancellationToken = default);
}
