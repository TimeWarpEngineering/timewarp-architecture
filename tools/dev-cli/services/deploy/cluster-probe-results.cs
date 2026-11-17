#region Purpose
// What the process half observed for one kubectl context.
#endregion

#region Design
// Input of AspireDeployCluster.CollectClusterProblems; the process half only gathers these, the pure half decides.
#endregion

namespace DevCli.Services;

/// <summary>
/// What the process half observed for one kubectl context: the API reachability probe, <c>kind get clusters</c> (kind contexts
/// only) and the registry probe (only when <see cref="AspireDeployCluster.RegistryToProbe"/> named an endpoint).
/// </summary>
internal sealed record ClusterProbeResults(
  string Context,
  bool Reachable,
  string ReachabilityError,
  bool KindProbeSucceeded = false,
  bool KindTimedOut = false,
  string KindOutput = "",
  string? RegistryEndpoint = null,
  bool RegistryAnswered = false,
  string? RegistryFailure = null);
