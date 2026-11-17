#region Purpose
// Argument builders, parsers and refusals for the kubernetes target's helm, kubectl, kind and
// registry probes, and the precedence of the cluster refusals.
#endregion

#region Design
// Kubernetes preflight requires the context's API to answer, and for a kind context (kind-<cluster>) that the
// kind cluster exists and the registry at registry-endpoint answers its /v2/ API (an HTTP probe, so still no
// container CLI). The target needs Helm 4.2+ (Aspire's helm upgrade --install).
//
// Which cluster checks run and which refusals reach the report (CollectClusterProblems,
// CollectKubernetesProblems) is decided here, so it is tested; the process half only gathers probe
// results. Every probe has a timeout (ProbeTimeout): an exec credential plugin waiting for an
// interactive login or a wedged Docker daemon is reported as timed out, not waited on.
//
// The process half (services/aspire-deploy-preflight.cs) runs the probes with these argument builders and hands
// the results back.
#endregion

namespace DevCli.Services;

/// <summary>Argument builders, parsers and refusals for the helm, kubectl, kind and registry probes of the kubernetes target.</summary>
internal static class AspireDeployCluster
{
  internal static readonly Version MinimumHelmVersion = new(4, 2);
  internal const string KindContextPrefix = "kind-";
  internal const string ReachabilityTimeout = "5s";

  /// <summary>Upper bound for each read-only preflight probe (kubectl, kind, helm, az, dotnet user-secrets).</summary>
  internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);

  /// <summary>First non-blank line of <paramref name="text"/>, else <paramref name="fallback"/>.</summary>
  internal static string FirstLine(string text, string fallback) =>
    text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fallback;

  /// <summary>Refusal for a probe that hit <see cref="ProbeTimeout"/>.</summary>
  internal static string ProbeTimedOutMessage(string executable, IReadOnlyList<string> arguments) =>
    $"`{executable} {string.Join(' ', arguments)}` timed out after {ProbeTimeout.TotalSeconds:0}s (an exec credential plugin waiting for "
    + "an interactive login, or a wedged Docker daemon?). Make it answer from this shell, then re-run.";

  internal static string[] BuildHelmVersionArguments() => ["version", "--short"];

  internal static string[] BuildKubectlContextArguments() => ["config", "current-context"];

  /// <summary>Reads the API server's <c>/version</c> through <paramref name="context"/>: does the cluster answer at all.</summary>
  internal static string[] BuildKubectlReachabilityArguments(string context) =>
    ["--context", context, "get", "--raw", "/version", $"--request-timeout={ReachabilityTimeout}"];

  /// <summary>Error when the context's API did not answer; null when it did.</summary>
  internal static string? ValidateKubectlReachable(bool probeSucceeded, string context, string error)
  {
    if (probeSucceeded)
    {
      return null;
    }

    string reason = FirstLine(error, "no response");
    return $"The kubectl context {context} does not answer (`kubectl --context {context} get --raw /version` failed: {reason}). "
      + "Start or recreate the cluster, or switch context with `kubectl config use-context <name>`.";
  }

  /// <summary>The kind cluster a <c>kind-&lt;cluster&gt;</c> context points at; null for any other context.</summary>
  internal static string? KindClusterName(string context) =>
    context.StartsWith(KindContextPrefix, StringComparison.Ordinal) && context.Length > KindContextPrefix.Length
      ? context[KindContextPrefix.Length..]
      : null;

  internal static string[] BuildKindClustersArguments() => ["get", "clusters"];

  /// <summary>Error when kind is missing or <c>kind get clusters</c> does not list <paramref name="cluster"/>; null when it does.</summary>
  internal static string? ValidateKindCluster(bool probeSucceeded, string output, string cluster)
  {
    if (!probeSucceeded)
    {
      return $"The kubectl context {KindContextPrefix}{cluster} is a kind context, but kind is not on PATH or `kind get clusters` failed. Install kind, or switch context.";
    }

    bool exists = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Any(line => string.Equals(line, cluster, StringComparison.Ordinal));
    return exists
      ? null
      : $"The kind cluster '{cluster}' (context {KindContextPrefix}{cluster}) does not exist — `kind get clusters` does not list it. "
        + $"Create it (see Local Kubernetes with kind in the tw-deploy skill), or delete the stale context: kubectl config delete-context {KindContextPrefix}{cluster}";
  }

  /// <summary>The registry's <c>/v2/</c> API for <paramref name="endpoint"/> (<c>host:port</c>, or a URL); null when unparseable.</summary>
  internal static Uri? BuildRegistryProbeUri(string endpoint)
  {
    string trimmed = endpoint.Trim().TrimEnd('/');
    string withScheme = trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : $"http://{trimmed}";
    return Uri.TryCreate(withScheme, UriKind.Absolute, out Uri? root) && root.Scheme is "http" or "https"
      ? new Uri(root, "/v2/")
      : null;
  }

  /// <summary>
  /// Error when the registry at <paramref name="endpoint"/> did not answer (any HTTP status counts as answering); null when it
  /// did. <paramref name="failure"/> is why (e.g. a TLS error), when known.
  /// </summary>
  internal static string? ValidateRegistry(bool answered, string endpoint, string appHostProject, string? failure = null) =>
    answered
      ? null
      : $"The container registry at {endpoint} ({AspireDeployParameters.RegistryEndpointParameter}) does not answer{(string.IsNullOrWhiteSpace(failure) ? "" : $" ({failure.Trim()})")}. "
        + "For kind, start the local registry container (kind-registry in the tw-deploy skill's kind recipe), or point the parameter at a running registry: "
        + $"dotnet user-secrets set '{AspireDeployParameters.ParameterConfigurationKey(AspireDeployParameters.RegistryEndpointParameter)}' '<host:port>' --project '{appHostProject}'";

  /// <summary>
  /// The registry to probe for <paramref name="context"/>: <c>registry-endpoint</c> when deploying (<paramref name="deploying"/>)
  /// to a kind context and the parameter resolved; null otherwise (deprovision, a non-kind context, or the parameter is unset).
  /// </summary>
  internal static string? RegistryToProbe(string context, DeployParameterResolution parameters, bool deploying) =>
    deploying && KindClusterName(context) is not null
      ? parameters.Resolved.FirstOrDefault(parameter => parameter.Name == AspireDeployParameters.RegistryEndpointParameter)?.Value
      : null;

  /// <summary>
  /// The cluster refusals from the probe results: a missing kind cluster is reported instead of the unreachable API (it is
  /// the root cause); the registry refusal only when a registry was probed (see <see cref="RegistryToProbe"/>).
  /// </summary>
  internal static string[] CollectClusterProblems(ClusterProbeResults probes, string appHostProject)
  {
    string? kindCluster = KindClusterName(probes.Context);
    string? kindError = kindCluster is null ? null : probes.KindTimedOut
      ? ProbeTimedOutMessage("kind", BuildKindClustersArguments())
      : ValidateKindCluster(probes.KindProbeSucceeded, probes.KindOutput, kindCluster);
    string? clusterError = kindError ?? ValidateKubectlReachable(probes.Reachable, probes.Context, probes.ReachabilityError);
    string? registryError = kindCluster is not null && probes.RegistryEndpoint is not null
      ? ValidateRegistry(probes.RegistryAnswered, probes.RegistryEndpoint, appHostProject, probes.RegistryFailure)
      : null;
    return [.. new[] { clusterError, registryError }.OfType<string>()];
  }

  /// <summary>
  /// Every kubernetes refusal in one list: parameter problems, helm, then no context or the cluster problems (only checked
  /// when a context is set).
  /// </summary>
  internal static string[] CollectKubernetesProblems(
    IReadOnlyList<string> parameterProblems, string? helmError, string? contextError, string? context, IReadOnlyList<string> clusterProblems) =>
  [
    .. parameterProblems,
    .. helmError is null ? (string[])[] : [helmError],
    .. context is null ? [contextError ?? NoKubectlContextMessage] : clusterProblems,
  ];

  /// <summary>Parses <c>helm version --short</c> output (e.g. <c>v4.2.0+g1a2b3c4</c>).</summary>
  internal static Version? ParseHelmVersion(string versionOutput)
  {
    string text = versionOutput.Trim().TrimStart('v', 'V');
    int end = text.IndexOfAny(['+', '-', ' ', '\n', '\r']);
    string core = end < 0 ? text : text[..end];
    return Version.TryParse(core, out Version? version) ? version : null;
  }

  /// <summary>Error when Helm (from <c>helm version --short</c>) is missing, unparseable or older than 4.2; null when usable.</summary>
  internal static string? ValidateHelm(bool probeSucceeded, string versionOutput)
  {
    if (!probeSucceeded)
    {
      return $"helm was not found on PATH or `helm version` failed. The kubernetes target needs Helm {MinimumHelmVersion} or later on PATH.";
    }

    Version? version = ParseHelmVersion(versionOutput);
    if (version is null)
    {
      return $"Could not determine the Helm version from `helm version --short` output '{versionOutput.Trim()}'. "
        + $"The kubernetes target needs Helm {MinimumHelmVersion} or later.";
    }

    return version < MinimumHelmVersion
      ? $"The kubernetes target needs Helm {MinimumHelmVersion} or later (installed: {version})."
      : null;
  }

  /// <summary>The current kubectl context, or null when kubectl failed or no context is set.</summary>
  internal static string? ParseKubectlContext(bool probeSucceeded, string output)
  {
    string context = output.Trim();
    return probeSucceeded && context.Length > 0 && !context.Contains('\n', StringComparison.Ordinal) ? context : null;
  }

  internal const string NoKubectlContextMessage =
    "No current kubectl context (kubectl is not on PATH or `kubectl config current-context` failed). Point kubectl at the target "
    + "cluster first, e.g. `kubectl config use-context <name>` (a local kind cluster is just a context).";
}
