#region Purpose
// Process half of `dev deploy` / `dev deprovision` preflight: the aspire, helm and kubectl probes,
// and reading this AppHost's deployment record from disk.
#endregion

#region Design
// Kept apart from aspire-deploy.cs so that file stays pure for dev-cli-tests (same split as
// aspire-cli.cs / aspire-run.cs). Probes only read: `aspire --version`, `helm version --short`,
// `kubectl config current-context`. Nothing here deploys, contacts a cluster API, creates a cluster
// or calls a container CLI. The kubernetes target needs Helm 4.2+ (Aspire's helm upgrade --install)
// and a current kubectl context, which is printed so the operator sees where the deploy goes; compose
// needs neither and reports the container runtime Aspire will use (ASPIRE_CONTAINER_RUNTIME).
#endregion

namespace DevCli.Services;

/// <summary>Resolved preflight for one deploy target: the AppHost, and the detail line printed before acting.</summary>
internal sealed record DeployPreflight(string RepoRoot, string AppHostProject, DeployTarget Target, string Detail);

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
    if (target == AspireDeploy.Kubernetes)
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

    return new DeployPreflight(repoRoot, appHostProject, target, detail);
  }

  /// <summary>Path of this AppHost's Production deployment-state file.</summary>
  internal static string StatePath(string appHostProject) =>
    AspireDeploy.DeploymentStatePath(
      AspireDeploy.AspireHome(
        Environment.GetEnvironmentVariable(AspireDeploy.AspireHomeVariable),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
      AspireDeploy.AppHostPath(appHostProject));

  /// <summary>What <c>aspire deploy</c> recorded for <paramref name="target"/> on this machine, or null.</summary>
  internal static async Task<DeploymentRecord?> ReadRecordAsync(string statePath, DeployTarget target, CancellationToken cancellationToken)
  {
    string? state = File.Exists(statePath) ? await File.ReadAllTextAsync(statePath, cancellationToken) : null;
    string migrationPath = AspireDeploy.MigrationStatePath(statePath);
    string? migration = File.Exists(migrationPath) ? await File.ReadAllTextAsync(migrationPath, cancellationToken) : null;
    return AspireDeploy.FindRecord(statePath, state, migration, target);
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
