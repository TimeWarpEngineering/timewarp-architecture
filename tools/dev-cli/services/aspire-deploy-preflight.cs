#region Purpose
// Process half of `dev deploy` / `dev deprovision` preflight: the aspire, helm, kubectl and az probes.
#endregion

#region Design
// Kept apart from aspire-deploy.cs so that file stays pure for dev-cli-tests (same split as
// aspire-cli.cs / aspire-run.cs). Probes only read: `aspire --version`, `helm version --short`,
// `kubectl config current-context`, `kubectl get --raw /version`, `kind get clusters`, an HTTP GET of
// the registry's /v2/, `az account show`, `dotnet user-secrets list --project <apphost>`, and the
// AppHost's appsettings.Production.json / appsettings.json (read as text; parsed by the pure half). Each process
// probe is bounded by AspireDeploy.ProbeTimeout (a timed-out probe is a refusal saying so). Nothing here
// deploys, creates a cluster or calls a container CLI. The kubernetes target needs Helm 4.2+ (Aspire's
// helm upgrade --install), a current kubectl context whose API answers (printed so the operator sees
// where the deploy goes), for a kind context the kind cluster, and for `dev deploy` (requireParameters)
// every required deploy parameter plus — on kind — a registry answering at registry-endpoint (task
// 286); `dev deprovision` resolves the parameters too but only forwards the ones set. This file only
// gathers probe results; which refusals they produce, and their precedence, is decided by the pure
// AspireDeploy.Collect*Problems (tested). Kubernetes problems are collected into one report instead of
// stopping at the first; aca needs `az login` and prints the
// subscription and its source (Azure__SubscriptionId, else the AppHost user secret
// Azure:SubscriptionId, else the az CLI's — only that last one is passed to Aspire, task 070-007);
// compose needs none of these and reports the container runtime Aspire will use
// (ASPIRE_CONTAINER_RUNTIME).
#endregion

namespace DevCli.Services;

/// <summary>
/// Resolved preflight for one deploy target: the AppHost, the detail line printed before acting, and the environment
/// variables the aspire process needs (aca: Azure__SubscriptionId when only the az CLI chose the subscription).
/// </summary>
internal sealed record DeployPreflight(
  string RepoRoot, string AppHostProject, DeployTarget Target, string Detail, IReadOnlyDictionary<string, string> AspireEnvironment,
  IReadOnlyList<ResolvedDeployParameter> Parameters);

/// <summary>Runs the read-only probes both deploy verbs share.</summary>
internal static class AspireDeployPreflight
{
  /// <summary>
  /// Resolves the AppHost and target and runs the probes; null after printing the refusal. Both verbs resolve the target's
  /// deploy parameters; <paramref name="requireParameters"/> (dev deploy) refuses when any is unset and probes the kind
  /// registry, while dev deprovision only forwards the ones that are set.
  /// </summary>
  internal static async Task<DeployPreflight?> RunAsync(ITerminal terminal, string verb, string? targetName, bool requireParameters, CancellationToken cancellationToken)
  {
    string? repoRoot = Git.FindRoot();
    if (repoRoot is null)
    {
      return Fail(terminal, "Error: could not find repository root.");
    }

    DeployTarget? target = AspireDeploy.ResolveTarget(targetName);
    if (target is null)
    {
      return Fail(terminal, $"Error: {AspireDeploy.UnknownTargetMessage(targetName!)}");
    }

    string appHostProject = Path.GetFullPath(Path.Combine(repoRoot, AspireRun.AppHostProject));
    if (!File.Exists(appHostProject))
    {
      return Fail(terminal, $"Error: AppHost project not found at {appHostProject}.");
    }

    string? versionError = await AspireCli.ValidateVersionAsync(repoRoot, AspireDeploy.MinimumCliVersion, $"`{verb}`", cancellationToken);
    if (versionError is not null)
    {
      return Fail(terminal, $"Error: {versionError}");
    }

    string detail;
    Dictionary<string, string> aspireEnvironment = [];
    string[] parameterProblems = [];
    DeployParameterResolution parameters = new([], []);
    if (AspireDeploy.RequiredParameters(target).Length > 0)
    {
      string[] secretsArguments = AspireDeploy.BuildUserSecretsListArguments(appHostProject);
      CommandOutput? secrets = await ProbeAsync("dotnet", secretsArguments, cancellationToken);
      parameters = AspireDeploy.ResolveParameters(
        target, AspireDeploy.CaseInsensitiveEnvironment(EnvironmentVariables()), secrets?.Success == true, secrets?.Stdout ?? "",
        ReadAppSettings(appHostProject));
      if (requireParameters)
      {
        string? secretsFailure = secrets switch
        {
          null => "dotnet is not on PATH",
          { TimedOut: true } => $"timed out after {AspireDeploy.ProbeTimeout.TotalSeconds:0}s",
          { Success: false } => AspireDeploy.FirstLine(secrets.Stderr, AspireDeploy.FirstLine(secrets.Stdout, $"exit {secrets.ExitCode}")),
          _ => null,
        };
        parameterProblems = AspireDeploy.CollectParameterProblems(appHostProject, target, parameters, secretsFailure);
      }
    }

    if (target == AspireDeploy.ContainerApps)
    {
      string[] azArguments = AspireDeploy.BuildAzureAccountArguments();
      CommandOutput? az = await ProbeAsync("az", azArguments, cancellationToken);
      AzureAccount? account = AspireDeploy.ParseAzureAccount(az?.Success == true, az?.Stdout ?? "");
      if (account is null)
      {
        return Fail(terminal, $"Error: {(az?.TimedOut == true ? AspireDeploy.ProbeTimedOutMessage("az", azArguments) : AspireDeploy.NoAzureLoginMessage)}");
      }

      string? environmentSubscription = Environment.GetEnvironmentVariable(AspireDeploy.AzureSubscriptionIdVariable);
      string? userSecretSubscription = null;
      if (string.IsNullOrWhiteSpace(environmentSubscription))
      {
        CommandOutput? secrets = await ProbeAsync("dotnet", AspireDeploy.BuildUserSecretsListArguments(appHostProject), cancellationToken);
        userSecretSubscription = AspireDeploy.ParseUserSecret(
          secrets?.Success == true, secrets?.Stdout ?? "", AspireDeploy.AzureSubscriptionIdConfigurationKey);
      }

      AzureSubscriptionChoice subscription = AspireDeploy.ChooseAzureSubscription(environmentSubscription, userSecretSubscription, account);
      if (subscription.PassToAspire)
      {
        aspireEnvironment[AspireDeploy.AzureSubscriptionIdVariable] = subscription.SubscriptionId;
      }

      detail = subscription.Detail;
    }
    else if (target == AspireDeploy.Kubernetes)
    {
      string[] helmArguments = AspireDeploy.BuildHelmVersionArguments();
      CommandOutput? helm = await ProbeAsync("helm", helmArguments, cancellationToken);
      string? helmError = helm?.TimedOut == true
        ? AspireDeploy.ProbeTimedOutMessage("helm", helmArguments)
        : AspireDeploy.ValidateHelm(helm?.Success == true, helm?.Stdout ?? "");

      string[] contextArguments = AspireDeploy.BuildKubectlContextArguments();
      CommandOutput? kubectl = await ProbeAsync("kubectl", contextArguments, cancellationToken);
      string? context = AspireDeploy.ParseKubectlContext(kubectl?.Success == true, kubectl?.Stdout ?? "");
      string? contextError = kubectl?.TimedOut == true ? AspireDeploy.ProbeTimedOutMessage("kubectl", contextArguments) : null;

      string[] clusterProblems = context is null
        ? []
        : AspireDeploy.CollectClusterProblems(await ProbeClusterAsync(context, parameters, requireParameters, cancellationToken), appHostProject);

      string[] problems = AspireDeploy.CollectKubernetesProblems(parameterProblems, helmError, contextError, context, clusterProblems);
      if (problems.Length > 0)
      {
        return FailReport(terminal, verb, problems);
      }

      detail = $"kubectl context: {context} (helm {helm!.Stdout.Trim()})";
    }
    else
    {
      string runtime = AspireDeploy.ContainerRuntime(Environment.GetEnvironmentVariable(AspireDeploy.ContainerRuntimeVariable));
      detail = $"Container runtime: {runtime} ({AspireDeploy.ContainerRuntimeVariable} selects another)";
    }

    if (parameterProblems.Length > 0)
    {
      return FailReport(terminal, verb, parameterProblems);
    }

    return new DeployPreflight(repoRoot, appHostProject, target, detail, aspireEnvironment, parameters.Resolved);
  }

  /// <summary>Gathers the cluster probe results; which refusals they produce is AspireDeploy.CollectClusterProblems'.</summary>
  private static async Task<ClusterProbeResults> ProbeClusterAsync(
    string context, DeployParameterResolution parameters, bool deploying, CancellationToken cancellationToken)
  {
    CommandOutput? reach = await ProbeAsync("kubectl", AspireDeploy.BuildKubectlReachabilityArguments(context), cancellationToken);
    string reachError = reach switch
    {
      null => "kubectl is not on PATH",
      { TimedOut: true } => $"timed out after {AspireDeploy.ProbeTimeout.TotalSeconds:0}s",
      _ => reach.Stderr,
    };
    ClusterProbeResults probes = new(context, reach?.Success == true, reachError);

    if (AspireDeploy.KindClusterName(context) is not null)
    {
      CommandOutput? kind = await ProbeAsync("kind", AspireDeploy.BuildKindClustersArguments(), cancellationToken);
      probes = probes with { KindProbeSucceeded = kind?.Success == true, KindTimedOut = kind?.TimedOut == true, KindOutput = kind?.Stdout ?? "" };
    }

    string? registry = AspireDeploy.RegistryToProbe(context, parameters, deploying);
    if (registry is not null)
    {
      (bool answered, string? failure) = await RegistryAnswersAsync(registry, cancellationToken);
      probes = probes with { RegistryEndpoint = registry, RegistryAnswered = answered, RegistryFailure = failure };
    }

    return probes;
  }

  /// <summary>GETs the registry's /v2/; any HTTP status (401 included) means something is listening. Otherwise, why not.</summary>
  private static async Task<(bool Answered, string? Failure)> RegistryAnswersAsync(string endpoint, CancellationToken cancellationToken)
  {
    Uri? uri = AspireDeploy.BuildRegistryProbeUri(endpoint);
    if (uri is null)
    {
      return (false, "not a host:port or http(s) URL");
    }

    using System.Net.Http.HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
    try
    {
      using System.Net.Http.HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);
      return (true, null);
    }
    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      return (false, $"no answer from {uri} within {client.Timeout.TotalSeconds:0}s");
    }
    catch (System.Net.Http.HttpRequestException exception) when (!cancellationToken.IsCancellationRequested)
    {
      // e.g. "The SSL connection could not be established" with the certificate problem as the inner exception.
      return (false, exception.InnerException is null ? exception.Message : $"{exception.Message} {exception.InnerException.Message}");
    }
  }

  /// <summary>The process environment as a dictionary, for the case-insensitive Parameters__&lt;name&gt; lookup.</summary>
  private static Dictionary<string, string> EnvironmentVariables()
  {
    Dictionary<string, string> variables = new(StringComparer.Ordinal);
    foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
    {
      if (entry.Key is string name && entry.Value is string value)
      {
        variables[name] = value;
      }
    }

    return variables;
  }

  private static DeployPreflight? FailReport(ITerminal terminal, string verb, IReadOnlyList<string> problems)
  {
    foreach (string line in AspireDeploy.BuildPreflightReport(verb, problems))
    {
      terminal.WriteErrorLine(line.Red());
    }

    Environment.ExitCode = 1;
    return null;
  }

  /// <summary>
  /// Runs a read-only probe bounded by <see cref="AspireDeploy.ProbeTimeout"/> (a timed-out probe reports TimedOut and fails);
  /// null when <paramref name="executable"/> is not on PATH.
  /// </summary>
  private static async Task<CommandOutput?> ProbeAsync(string executable, string[] arguments, CancellationToken cancellationToken) =>
    PathResolver.ResolveExecutable(executable) is null
      ? null
      : await Shell.Builder(executable)
        .WithArguments(arguments)
        .WithTimeout(AspireDeploy.ProbeTimeout)
        .WithNoValidation()
        .CaptureAsync(cancellationToken);

  /// <summary>The AppHost appsettings files beside <paramref name="appHostProject"/> that exist, highest precedence first.</summary>
  private static AppSettingsFile[] ReadAppSettings(string appHostProject)
  {
    string directory = Path.GetDirectoryName(appHostProject)!;
    return
    [
      .. AspireDeploy.AppSettingsFileNames()
        .Select(fileName => (FileName: fileName, Path: Path.Combine(directory, fileName)))
        .Where(file => File.Exists(file.Path))
        .Select(file => new AppSettingsFile(file.FileName, File.ReadAllText(file.Path))),
    ];
  }

  private static DeployPreflight? Fail(ITerminal terminal, string message)
  {
    terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = 1;
    return null;
  }
}
