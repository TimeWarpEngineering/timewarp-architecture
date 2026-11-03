#region Purpose
// Deploy targets and the `aspire deploy` / `aspire destroy` argument builders for `dev deploy` /
// `dev deprovision` (manual `aspire deploy` / `aspire destroy` per publish target).
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and gate
// both verbs without deploying anything. The process half (the aspire/helm/kubectl/az probes) lives in
// services/aspire-deploy-preflight.cs.
//
// The former single AspireDeploy class is split by responsibility, all in services/deploy/:
// AspireDeploy (targets, deploy/destroy arguments), AspireDeployParameters, AspireDeploySecrets,
// AspireDeployCluster, AspireDeployAzure, AspireDeployEnvironment and AspireDeployText.
//
// Deploying is an operator action, never CI (task 070-006): no workflow step or `dev workflow` mode
// calls these verbs. The target is the AppHost's Publish:Target switch (program.cs), passed through
// as `-- --Publish:Target=<name>` exactly like `dev publish`; the default is the AppHost's default
// (compose).
//
// Destroying is Aspire's: `dev deprovision` runs `aspire destroy` and trusts it. `aspire destroy`
// only knows deployments `aspire deploy` recorded on THIS machine, so on failure the verb prints the
// manual removal for the target (BuildManualCleanupLines) and exits with Aspire's exit code. The dev
// CLI never inspects Aspire's own deployment state and never runs the manual removal itself.
//
// Runtime neutrality (task 277): nothing here calls a container CLI. Compose deploy and destroy are
// Aspire's, which honour ASPIRE_CONTAINER_RUNTIME; the runtime name only appears in printed text.
#endregion

namespace DevCli.Services;

/// <summary>Targets and the <c>aspire deploy</c> / <c>aspire destroy</c> argument builders for <c>dev deploy</c> / <c>dev deprovision</c>.</summary>
internal static class AspireDeploy
{
  internal static readonly Version MinimumCliVersion = new(13, 6);

  internal const string DeployEnvironment = "Production";
  internal const string ContainerRuntimeVariable = "ASPIRE_CONTAINER_RUNTIME";
  internal const string DefaultContainerRuntime = "docker";
  internal const string PostgresClaimName = "postgres-data";
  internal const string PostgresKeyVaultTag = "postgres-kv";

  internal static readonly DeployTarget Compose = new("compose");
  internal static readonly DeployTarget Kubernetes = new("kubernetes");
  internal static readonly DeployTarget ContainerApps = new("aca");
  internal static readonly DeployTarget ContainerAppsNeon = new("aca-neon");

  internal static readonly DeployTarget[] Targets = [Compose, Kubernetes, ContainerApps, ContainerAppsNeon];

  /// <summary>aca and aca-neon both provision an Azure Container Apps environment.</summary>
  internal static bool IsAzureContainerApps(DeployTarget target) => target == ContainerApps || target == ContainerAppsNeon;

  /// <summary>Flexible Server and its Key Vault exist only on the aca target.</summary>
  internal static bool UsesFlexibleServer(DeployTarget target) => target == ContainerApps;

  /// <summary>The published EF bundle is the migration artifact for both Azure Container Apps targets.</summary>
  internal static bool UsesMigrationBundle(DeployTarget target) => IsAzureContainerApps(target);

  /// <summary>The AppHost's Publish:Target default.</summary>
  internal static DeployTarget DefaultTarget => Compose;

  /// <summary>Resolves <paramref name="name"/> (null = the default); null when it is not a target.</summary>
  internal static DeployTarget? ResolveTarget(string? name) =>
    string.IsNullOrWhiteSpace(name)
      ? DefaultTarget
      : Targets.FirstOrDefault(target => string.Equals(target.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

  internal static string UnknownTargetMessage(string name) =>
    $"Unknown deploy target '{name}'. Valid targets: {string.Join(", ", Targets.Select(target => target.Name))} (default: {DefaultTarget.Name}).";

  /// <summary>
  /// <c>aspire deploy</c>; <c>--non-interactive</c> only when the operator passed <c>--yes</c>. The resolved deploy
  /// parameters follow the target after <c>--</c> as <c>--Parameters:&lt;name&gt;=&lt;value&gt;</c>.
  /// </summary>
  internal static string[] BuildDeployArguments(string appHostProject, DeployTarget target, bool nonInteractive, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    "deploy", "--apphost", appHostProject, "--environment", DeployEnvironment,
    .. nonInteractive ? (string[])["--non-interactive"] : [],
    "--", $"--Publish:Target={target.Name}",
    .. parameters.Select(parameter => $"--{AspireDeployParameters.ParameterConfigurationKey(parameter.Name)}={parameter.Value}"),
  ];

  /// <summary>
  /// Same arguments as the parameter overload. <paramref name="secrets"/> are not forwarded.
  /// A secret name on <paramref name="parameters"/> is refused.
  /// </summary>
  internal static string[] BuildDeployArguments(
    string appHostProject,
    DeployTarget target,
    bool nonInteractive,
    IReadOnlyList<ResolvedDeployParameter> parameters,
    IReadOnlyList<ResolvedDeployParameter> secrets)
  {
    RejectSecretsOnParameters(parameters, secrets);
    return BuildDeployArguments(appHostProject, target, nonInteractive, parameters);
  }

  /// <summary>
  /// aca migrate still needs <c>az login</c>. aca-neon migrate uses only the direct Neon string.
  /// </summary>
  internal static bool ProbesAzureAccount(DeployTarget target, PreflightScope scope) =>
    IsAzureContainerApps(target) && !(target == ContainerAppsNeon && scope == PreflightScope.Migrate);

  /// <summary>
  /// <c>aspire destroy</c>; <c>--yes --non-interactive</c> only when the operator passed <c>--yes</c>, else Aspire asks. Any
  /// deploy parameter the operator has set follows the target as <c>--Parameters:&lt;name&gt;=&lt;value&gt;</c>.
  /// </summary>
  internal static string[] BuildDestroyArguments(string appHostProject, DeployTarget target, bool yes, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    "destroy", "--apphost", appHostProject, "--environment", DeployEnvironment,
    .. yes ? (string[])["--yes", "--non-interactive"] : [],
    "--", $"--Publish:Target={target.Name}",
    .. parameters.Select(parameter => $"--{AspireDeployParameters.ParameterConfigurationKey(parameter.Name)}={parameter.Value}"),
  ];

  /// <summary>Container runtime Aspire uses for Compose: ASPIRE_CONTAINER_RUNTIME, else docker.</summary>
  internal static string ContainerRuntime(string? configured) =>
    string.IsNullOrWhiteSpace(configured) ? DefaultContainerRuntime : configured.Trim();

  internal static void RejectSecretsOnParameters(
    IReadOnlyList<ResolvedDeployParameter> parameters,
    IReadOnlyList<ResolvedDeployParameter> secrets)
  {
    foreach (ResolvedDeployParameter secret in secrets)
    {
      if (parameters.Any(parameter => string.Equals(parameter.Name, secret.Name, StringComparison.Ordinal)))
      {
        throw new InvalidOperationException(
          $"Secret parameter '{secret.Name}' is on the deploy parameter list. Secrets are copied as Parameters__ environment variables and are not forwarded.");
      }
    }
  }
}
