#region Purpose
// Where `dev open` sends the browser, and whether it keeps a kubectl port-forward running.
#endregion

#region Design
// Returned by DeployKubernetes.DecideKubernetesOpen.
#endregion

namespace DevCli.Services;

/// <summary>Where <c>dev open</c> sends the browser, and whether it must keep a kubectl port-forward running for it.</summary>
internal sealed record OpenPlan(string Url, bool PortForward, string Detail);
