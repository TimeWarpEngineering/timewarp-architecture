#region Purpose
// Targets, argument builders, preflight parsing, the deployment-record lookup and the operator text
// for `dev deploy` / `dev deprovision` (manual `aspire deploy` / `aspire destroy` per publish target).
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and gate
// both verbs without deploying anything. The process half (the aspire/helm/kubectl probes) lives in
// services/aspire-deploy-preflight.cs.
//
// Deploying is an operator action, never CI (task 070-006): no workflow step or `dev workflow` mode
// calls these verbs. The target is the AppHost's Publish:Target switch (program.cs), passed through
// as `-- --Publish:Target=<name>` exactly like `dev publish`; the default is the AppHost's default
// (compose). The environment-resource names (compose, k8s) duplicate the AppHost's
// ComposeEnvironmentResourceName / KubernetesEnvironmentResourceName because the dev CLI cannot
// reference the AppHost; aspire-tests' DeploymentRecordModel_Given_ compile-includes this file and
// proves both names, and the state-path hash, against the real AppHost model.
//
// The deployment record: `aspire destroy` only knows what `aspire deploy` recorded on THIS machine,
// in <ASPIRE_HOME or ~/.aspire>/deployments/<hash>/<environment>.json. Mirrors Aspire.Hosting 13.6
// (DistributedApplicationBuilder + FileDeploymentStateManager): for a .csproj AppHost the hash is
// upper-case hex SHA256 of the lower-cased AppHost path — the project directory joined with the
// AppHostProjectName assembly metadata, which is the .csproj file name, so the full .csproj path
// (the same AppHost:Path db-nuke.cs hashes); the file name is the lower-cased environment.
// The file holds flattened `Section:Key` entries; the Compose environment records
// `DockerCompose:<env>` (ComposeFilePath) and the Kubernetes environment `Helm:<env>` (ReleaseName,
// Namespace) — the same keys Aspire's own destroy steps read. A nested shape is accepted too, and a
// `<environment>.json.migration` companion's CurrentState wins over the file, as in Aspire's loader.
// Without a record `dev deprovision` never runs `aspire destroy` (it would report "nothing to
// destroy" and exit 0 while the stack keeps running): it says so and prints the manual removal.
// It never runs that removal itself.
//
// Runtime neutrality (task 277): nothing here calls a container CLI. Compose deploy and destroy are
// Aspire's, which honour ASPIRE_CONTAINER_RUNTIME; the runtime name only appears in printed text.
// The Compose project name Aspire uses is aspire-<env>-<first 8 of the AppHost path hash, lower>.
#endregion

namespace DevCli.Services;

/// <summary>One deploy target: the Publish:Target value and the deployment-record section its environment writes.</summary>
internal sealed record DeployTarget(string Name, string EnvironmentResourceName, string StateSectionPrefix, string RecordKey)
{
  /// <summary>Flattened deployment-state section, e.g. <c>DockerCompose:compose</c>.</summary>
  internal string StateSection => $"{StateSectionPrefix}:{EnvironmentResourceName}";
}

/// <summary>What <c>aspire deploy</c> recorded for one target.</summary>
internal sealed record DeploymentRecord(string StatePath, IReadOnlyDictionary<string, string> Values);

/// <summary>Builds and checks the <c>aspire deploy</c> / <c>aspire destroy</c> invocations for <c>dev deploy</c> / <c>dev deprovision</c>.</summary>
internal static class AspireDeploy
{
  internal static readonly Version MinimumCliVersion = new(13, 6);
  internal static readonly Version MinimumHelmVersion = new(4, 2);

  internal const string DeployEnvironment = "Production";
  internal const string ContainerRuntimeVariable = "ASPIRE_CONTAINER_RUNTIME";
  internal const string AspireHomeVariable = "ASPIRE_HOME";
  internal const string DefaultContainerRuntime = "docker";
  internal const string PostgresClaimName = "postgres-data";

  internal static readonly DeployTarget Compose = new("compose", "compose", "DockerCompose", "ComposeFilePath");
  internal static readonly DeployTarget Kubernetes = new("kubernetes", "k8s", "Helm", "ReleaseName");

  internal static readonly DeployTarget[] Targets = [Compose, Kubernetes];

  /// <summary>The AppHost's Publish:Target default.</summary>
  internal static DeployTarget DefaultTarget => Compose;

  /// <summary>Resolves <paramref name="name"/> (null = the default); null when it is not a target.</summary>
  internal static DeployTarget? ResolveTarget(string? name) =>
    string.IsNullOrWhiteSpace(name)
      ? DefaultTarget
      : Targets.FirstOrDefault(target => string.Equals(target.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

  internal static string UnknownTargetMessage(string name) =>
    $"Unknown deploy target '{name}'. Valid targets: {string.Join(", ", Targets.Select(target => target.Name))} (default: {DefaultTarget.Name}).";

  /// <summary><c>aspire deploy</c>; <c>--non-interactive</c> only when the operator passed <c>--yes</c>.</summary>
  internal static string[] BuildDeployArguments(string appHostProject, DeployTarget target, bool nonInteractive) =>
    nonInteractive
      ? ["deploy", "--apphost", appHostProject, "--environment", DeployEnvironment, "--non-interactive", "--", $"--Publish:Target={target.Name}"]
      : ["deploy", "--apphost", appHostProject, "--environment", DeployEnvironment, "--", $"--Publish:Target={target.Name}"];

  /// <summary><c>aspire destroy</c> — only ever built after the operator confirmed with <c>--yes</c>.</summary>
  internal static string[] BuildDestroyArguments(string appHostProject, DeployTarget target) =>
    ["destroy", "--apphost", appHostProject, "--environment", DeployEnvironment, "--non-interactive", "--yes", "--", $"--Publish:Target={target.Name}"];

  internal static string[] BuildHelmVersionArguments() => ["version", "--short"];

  internal static string[] BuildKubectlContextArguments() => ["config", "current-context"];

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

  /// <summary>Container runtime Aspire uses for Compose: ASPIRE_CONTAINER_RUNTIME, else docker.</summary>
  internal static string ContainerRuntime(string? configured) =>
    string.IsNullOrWhiteSpace(configured) ? DefaultContainerRuntime : configured.Trim();

  /// <summary>Aspire's <c>AppHost:Path</c> for a .csproj AppHost: the full .csproj path.</summary>
  internal static string AppHostPath(string appHostProject) => Path.GetFullPath(appHostProject);

  /// <summary>Aspire's <c>AppHost:DeploymentStatePathSha256</c> for a .csproj AppHost at <paramref name="appHostPath"/>.</summary>
  internal static string DeploymentStateHash(string appHostPath) =>
    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
      System.Text.Encoding.UTF8.GetBytes(appHostPath.ToLowerInvariant())));

  /// <summary><c>&lt;aspireHome&gt;/deployments/&lt;hash&gt;/&lt;environment&gt;.json</c>.</summary>
  internal static string DeploymentStatePath(string aspireHome, string appHostPath) =>
    Path.Combine(aspireHome, "deployments", DeploymentStateHash(appHostPath), $"{DeployEnvironment.ToLowerInvariant()}.json");

  /// <summary>ASPIRE_HOME when set, else <c>~/.aspire</c>.</summary>
  internal static string AspireHome(string? configured, string userProfile) =>
    string.IsNullOrWhiteSpace(configured) ? Path.Combine(userProfile, ".aspire") : configured;

  /// <summary>The Compose project name Aspire deploys this AppHost under.</summary>
  internal static string ComposeProjectName(string appHostPath) =>
    $"aspire-{Compose.EnvironmentResourceName}-{DeploymentStateHash(appHostPath)[..8].ToLowerInvariant()}";

  /// <summary>
  /// The record for <paramref name="target"/>: the <c>.migration</c> companion's CurrentState wins when
  /// it has one (as in Aspire's loader), else the state file. Null/empty text = that file is absent.
  /// Null unless the section carries the key Aspire's destroy step needs.
  /// </summary>
  internal static DeploymentRecord? FindRecord(string statePath, string? stateJson, string? migrationJson, DeployTarget target) =>
    FindRecordIn(statePath, MigrationCurrentState(migrationJson), target)
      ?? FindRecordIn(statePath, stateJson, target);

  internal static string MigrationStatePath(string statePath) => statePath + ".migration";

  private static string? MigrationCurrentState(string? migrationJson)
  {
    if (string.IsNullOrWhiteSpace(migrationJson)) return null;

    try
    {
      using var document = System.Text.Json.JsonDocument.Parse(migrationJson);
      if (!document.RootElement.TryGetProperty("CurrentState", out System.Text.Json.JsonElement current)) return null;
      return current.ValueKind switch
      {
        System.Text.Json.JsonValueKind.String => current.GetString(),
        System.Text.Json.JsonValueKind.Object => current.GetRawText(),
        _ => null,
      };
    }
    catch (System.Text.Json.JsonException)
    {
      return null;
    }
  }

  private static DeploymentRecord? FindRecordIn(string statePath, string? stateJson, DeployTarget target)
  {
    if (string.IsNullOrWhiteSpace(stateJson)) return null;

    Dictionary<string, string> flattened = new(StringComparer.OrdinalIgnoreCase);
    try
    {
      using var document = System.Text.Json.JsonDocument.Parse(stateJson);
      Flatten(document.RootElement, "", flattened);
    }
    catch (System.Text.Json.JsonException)
    {
      return null;
    }

    string prefix = target.StateSection + ":";
    var values = flattened
      .Where(entry => entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      .ToDictionary(entry => entry.Key[prefix.Length..], entry => entry.Value, StringComparer.OrdinalIgnoreCase);

    return values.TryGetValue(target.RecordKey, out string? recorded) && !string.IsNullOrWhiteSpace(recorded)
      ? new DeploymentRecord(statePath, values)
      : null;
  }

  private static void Flatten(System.Text.Json.JsonElement element, string path, Dictionary<string, string> into)
  {
    switch (element.ValueKind)
    {
      case System.Text.Json.JsonValueKind.Object:
        foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
          Flatten(property.Value, path.Length == 0 ? property.Name : $"{path}:{property.Name}", into);
        }

        break;
      case System.Text.Json.JsonValueKind.String:
        into[path] = element.GetString() ?? "";
        break;
      case System.Text.Json.JsonValueKind.Array:
        break;
      default:
        into[path] = element.GetRawText();
        break;
    }
  }

  /// <summary>What `dev deploy` is about to do, printed before the confirmation.</summary>
  internal static string[] BuildDeployPlanLines(string appHostProject, DeployTarget target, string preflightDetail) =>
  [
    $"dev deploy → aspire deploy ({target.Name})",
    $"  AppHost:     {appHostProject}",
    $"  Environment: {DeployEnvironment}",
    $"  Target:      Publish:Target={target.Name}",
    $"  {preflightDetail}",
  ];

  internal const string DeployConfirmationRefusal =
    "Not deploying: no confirmation. Re-run with --yes to deploy non-interactively, or from a terminal to answer the prompt.";

  /// <summary>Printed when the operator answers anything but yes at the deploy prompt.</summary>
  internal const string DeployDeclined = "Not deploying: declined at the prompt. Nothing was run.";

  /// <summary>Printed when `aspire deploy` left no record for <paramref name="target"/> on this machine.</summary>
  internal static string[] BuildNoRecordLines(DeployTarget target, string statePath, string appHostPath, string containerRuntime) =>
  [
    $"No {target.Name} deployment is recorded for this AppHost on this machine ({statePath}).",
    "`aspire destroy` only knows deployments made by `aspire deploy` from this checkout on this machine, so nothing was run and nothing was removed.",
    "If the stack is still running, remove it by hand — this deletes its data:",
    .. ManualRemovalLines(target, appHostPath, containerRuntime),
  ];

  private static string[] ManualRemovalLines(DeployTarget target, string appHostPath, string containerRuntime) =>
    target == Kubernetes
      ?
      [
        "  helm list --all-namespaces                                  # find the release and namespace",
        "  helm uninstall <release> --namespace <namespace>",
        "  kubectl get pvc --namespace <namespace>                     # the postgres data claim",
        $"  kubectl delete pvc {PostgresClaimName} --namespace <namespace>",
      ]
      :
      [
        $"  {containerRuntime} compose ls                                     # find the project",
        $"  {containerRuntime} compose --project-name {ComposeProjectName(appHostPath)} down --volumes",
        "  (that project name is this checkout's; a deployment from another checkout or machine has its own `aspire-compose-…` name)",
      ];

  /// <summary>Printed when a record exists but the operator did not pass --yes: what would be destroyed.</summary>
  internal static string[] BuildDeprovisionRefusalLines(DeployTarget target, DeploymentRecord record) =>
  [
    $"dev deprovision would run `aspire destroy` for the {target.Name} deployment recorded in {record.StatePath}:",
    .. record.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"  {entry.Key}: {entry.Value}"),
    target == Kubernetes
      ? "This uninstalls the Helm release; the postgres data volume and its data are deleted."
      : "This stops and removes the Compose stack's containers, networks and volumes — the postgres data is deleted.",
    "Nothing was run. Re-run with --yes to destroy it.",
  ];

  /// <summary>Kubernetes only: the claim can outlive `helm uninstall`; tell the operator how to check.</summary>
  internal static string[] BuildPostDestroyLines(DeployTarget target, DeploymentRecord record) =>
    target == Kubernetes
      ?
      [
        $"Check that the postgres claim is gone: kubectl get pvc --namespace {record.Values.GetValueOrDefault("Namespace", "<namespace>")}",
        $"If {PostgresClaimName} remains, delete it: kubectl delete pvc {PostgresClaimName} --namespace {record.Values.GetValueOrDefault("Namespace", "<namespace>")}",
      ]
      : [];
}
