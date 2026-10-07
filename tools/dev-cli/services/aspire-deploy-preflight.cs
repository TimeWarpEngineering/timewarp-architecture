#region Purpose
// Process half of `dev deploy` / `dev deprovision` preflight: the aspire, helm, kubectl and az probes.
#endregion

#region Design
// Kept apart from aspire-deploy.cs so that file stays pure for dev-cli-tests (same split as
// aspire-cli.cs / aspire-run.cs). Probes only read: `aspire --version`, `helm version --short`,
// `kubectl config current-context`, `az account show`. Nothing here deploys, contacts a cluster API,
// creates a cluster or calls a container CLI. The kubernetes target needs Helm 4.2+ (Aspire's helm
// upgrade --install) and a current kubectl context, which is printed so the operator sees where the
// deploy goes; aca needs `az login` and prints the subscription (Azure__SubscriptionId, else the az
// CLI's, which the verbs then pass to Aspire — task 070-007); compose needs none of these and reports
// the container runtime Aspire will use (ASPIRE_CONTAINER_RUNTIME).
#endregion

namespace DevCli.Services;

/// <summary>
/// Resolved preflight for one deploy target: the AppHost, the detail line printed before acting, and the environment
/// variables the aspire process needs (aca: Azure__SubscriptionId when the az CLI chose the subscription).
/// </summary>
internal sealed record DeployPreflight(string RepoRoot, string AppHostProject, DeployTarget Target, string Detail, IReadOnlyDictionary<string, string> AspireEnvironment);

/// <summary>Runs the read-only probes both deploy verbs share.</summary>
internal static class AspireDeployPreflight
{
  /// <summary>Resolves the AppHost and target and runs the probes; null after printing the refusal.</summary>
  internal static async Task<DeployPreflight?> RunAsync(ITerminal terminal, string verb, string? targetName, CancellationToken cancellationToken)
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
    if (target == AspireDeploy.ContainerApps)
    {
      CommandOutput? az = await ProbeAsync("az", AspireDeploy.BuildAzureAccountArguments(), cancellationToken);
      AzureAccount? account = AspireDeploy.ParseAzureAccount(az?.Success == true, az?.Stdout ?? "");
      if (account is null)
      {
        return Fail(terminal, $"Error: {AspireDeploy.NoAzureLoginMessage}");
      }

      AzureSubscriptionChoice subscription = AspireDeploy.ChooseAzureSubscription(
        Environment.GetEnvironmentVariable(AspireDeploy.AzureSubscriptionIdVariable), account);
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
        return Fail(terminal, $"Error: {helmError}");
      }

      CommandOutput? kubectl = await ProbeAsync("kubectl", AspireDeploy.BuildKubectlContextArguments(), cancellationToken);
      string? context = AspireDeploy.ParseKubectlContext(kubectl?.Success == true, kubectl?.Stdout ?? "");
      if (context is null)
      {
        return Fail(terminal, $"Error: {AspireDeploy.NoKubectlContextMessage}");
      }

      detail = $"kubectl context: {context} (helm {helm!.Stdout.Trim()})";
    }
    else
    {
      string runtime = AspireDeploy.ContainerRuntime(Environment.GetEnvironmentVariable(AspireDeploy.ContainerRuntimeVariable));
      detail = $"Container runtime: {runtime} ({AspireDeploy.ContainerRuntimeVariable} selects another)";
    }

    return new DeployPreflight(repoRoot, appHostProject, target, detail, aspireEnvironment);
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
