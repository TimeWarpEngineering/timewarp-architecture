#region Purpose
// What a verb's preflight checks.
#endregion

#region Design
// The four static instances are the four verbs; AspireDeploy helpers branch on them.
#endregion

namespace DevCli.Services;

/// <summary>
/// What a verb's preflight checks: deploy parameters (required, or resolved best-effort), the kind registry probe, the Aspire
/// CLI version and Helm. <c>dev open</c> / <c>dev deploy migrate</c> need neither aspire nor helm, only the target's
/// cluster or az login (task 287).
/// </summary>
internal sealed record PreflightScope(bool RequireParameters, bool ProbeRegistry, bool RequireAspire, bool RequireHelm)
{
  internal static readonly PreflightScope Deploy = new(RequireParameters: true, ProbeRegistry: true, RequireAspire: true, RequireHelm: true);
  internal static readonly PreflightScope Deprovision = new(RequireParameters: false, ProbeRegistry: false, RequireAspire: true, RequireHelm: true);
  internal static readonly PreflightScope Open = new(RequireParameters: false, ProbeRegistry: false, RequireAspire: false, RequireHelm: false);
  internal static readonly PreflightScope Migrate = new(RequireParameters: true, ProbeRegistry: false, RequireAspire: false, RequireHelm: false);
}
