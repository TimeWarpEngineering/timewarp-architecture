#region Purpose
// Process half of `dev deploy` / `dev deprovision` preflight: the aspire, helm, kubectl and az probes.
#endregion

#region Design
// Kept apart from aspire-deploy.cs so that file stays pure for dev-cli-tests (same split as
// aspire-cli.cs / aspire-run.cs). Probes only read: `aspire --version`, `helm version --short`,
// `kubectl config current-context`, `kubectl get --raw /version`, `kind get clusters`, an HTTP GET of
// the registry's /v2/, `az account show`, `dotnet user-secrets list --project <apphost>`. Nothing here
// deploys, creates a cluster or calls a container CLI. The kubernetes target needs Helm 4.2+ (Aspire's
// helm upgrade --install), a current kubectl context whose API answers (printed so the operator sees
// where the deploy goes), for a kind context the kind cluster, and for `dev deploy` (resolveParameters)
// every required deploy parameter plus — on kind — a registry answering at registry-endpoint (task
// 286). Kubernetes problems are collected into one report instead of stopping at the first; aca needs `az login` and prints the
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
  /// Resolves the AppHost and target and runs the probes; null after printing the refusal. <paramref name="resolveParameters"/>
  /// (dev deploy) also resolves the target's required deploy parameters and refuses when any is unset.
  /// </summary>
  internal static async Task<DeployPreflight?> RunAsync(ITerminal terminal, string verb, string? targetName, bool resolveParameters, CancellationToken cancellationToken)
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
    List<string> problems = [];
    DeployParameterResolution parameters = new([], []);
    if (resolveParameters && AspireDeploy.RequiredParameters(target).Length > 0)
    {
      CommandOutput? secrets = await ProbeAsync("dotnet", AspireDeploy.BuildUserSecretsListArguments(appHostProject), cancellationToken);
      parameters = AspireDeploy.ResolveParameters(target, Environment.GetEnvironmentVariable, secrets?.Success == true, secrets?.Stdout ?? "");
      if (parameters.Missing.Count > 0)
      {
        problems.Add(string.Join('\n', AspireDeploy.BuildMissingParameterLines(appHostProject, target, parameters.Missing)));
      }
    }

    if (target == AspireDeploy.ContainerApps)
    {
      CommandOutput? az = await ProbeAsync("az", AspireDeploy.BuildAzureAccountArguments(), cancellationToken);
      AzureAccount? account = AspireDeploy.ParseAzureAccount(az?.Success == true, az?.Stdout ?? "");
      if (account is null)
      {
        return Fail(terminal, $"Error: {AspireDeploy.NoAzureLoginMessage}");
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
      CommandOutput? helm = await ProbeAsync("helm", AspireDeploy.BuildHelmVersionArguments(), cancellationToken);
      string? helmError = AspireDeploy.ValidateHelm(helm?.Success == true, helm?.Stdout ?? "");
      if (helmError is not null)
      {
        problems.Add(helmError);
      }

      CommandOutput? kubectl = await ProbeAsync("kubectl", AspireDeploy.BuildKubectlContextArguments(), cancellationToken);
      string? context = AspireDeploy.ParseKubectlContext(kubectl?.Success == true, kubectl?.Stdout ?? "");
      if (context is null)
      {
        problems.Add(AspireDeploy.NoKubectlContextMessage);
      }
      else
      {
        await CheckClusterAsync(context, parameters, problems, cancellationToken);
      }

      if (problems.Count > 0)
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

    if (problems.Count > 0)
    {
      return FailReport(terminal, verb, problems);
    }

    return new DeployPreflight(repoRoot, appHostProject, target, detail, aspireEnvironment, parameters.Resolved);
  }

  /// <summary>The context's API answers; for a kind context, its cluster exists and (deploy) the registry answers.</summary>
  private static async Task CheckClusterAsync(string context, DeployParameterResolution parameters, List<string> problems, CancellationToken cancellationToken)
  {
    CommandOutput? reach = await ProbeAsync("kubectl", AspireDeploy.BuildKubectlReachabilityArguments(context), cancellationToken);
    string? reachError = AspireDeploy.ValidateKubectlReachable(reach?.Success == true, context, reach?.Stderr ?? "kubectl is not on PATH");

    string? kindCluster = AspireDeploy.KindClusterName(context);
    string? kindError = null;
    if (kindCluster is not null)
    {
      CommandOutput? kind = await ProbeAsync("kind", AspireDeploy.BuildKindClustersArguments(), cancellationToken);
      kindError = AspireDeploy.ValidateKindCluster(kind?.Success == true, kind?.Stdout ?? "", kindCluster);
    }

    // A missing kind cluster is why its API does not answer; report the root cause only.
    if (kindError is not null)
    {
      problems.Add(kindError);
    }
    else if (reachError is not null)
    {
      problems.Add(reachError);
    }

    ResolvedDeployParameter? registry = parameters.Resolved.FirstOrDefault(parameter => parameter.Name == AspireDeploy.RegistryEndpointParameter);
    if (kindCluster is not null && registry is not null)
    {
      string? registryError = AspireDeploy.ValidateRegistry(await RegistryAnswersAsync(registry.Value, cancellationToken), registry.Value);
      if (registryError is not null)
      {
        problems.Add(registryError);
      }
    }
  }

  /// <summary>GETs the registry's /v2/; any HTTP status (401 included) means something is listening.</summary>
  private static async Task<bool> RegistryAnswersAsync(string endpoint, CancellationToken cancellationToken)
  {
    Uri? uri = AspireDeploy.BuildRegistryProbeUri(endpoint);
    if (uri is null)
    {
      return false;
    }

    using System.Net.Http.HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
    try
    {
      using System.Net.Http.HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);
      return true;
    }
    catch (Exception exception) when (exception is System.Net.Http.HttpRequestException or TaskCanceledException)
    {
      return false;
    }
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

  /// <summary>Runs a read-only probe; null when <paramref name="executable"/> is not on PATH.</summary>
  private static async Task<CommandOutput?> ProbeAsync(string executable, string[] arguments, CancellationToken cancellationToken) =>
    PathResolver.ResolveExecutable(executable) is null
      ? null
      : await Shell.Builder(executable)
        .WithArguments(arguments)
        .WithNoValidation()
        .CaptureAsync(cancellationToken);

  private static DeployPreflight? Fail(ITerminal terminal, string message)
  {
    terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = 1;
    return null;
  }
}
