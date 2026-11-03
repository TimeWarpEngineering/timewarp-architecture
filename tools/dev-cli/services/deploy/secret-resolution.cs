#region Purpose
// Secret parameters for one target and which of them were committed to appsettings.
#endregion

#region Design
// Returned by AspireDeploySecrets.ResolveSecrets. A value found in appsettings is reported and never used.
#endregion

namespace DevCli.Services;

/// <summary>
/// Secret parameters for one target. CommittedToAppSettings names a value found in an appsettings file; that value is never used.
/// </summary>
internal sealed record SecretResolution(
  IReadOnlyList<ResolvedDeployParameter> Resolved,
  IReadOnlyList<string> Missing,
  IReadOnlyList<string> CommittedToAppSettings);
