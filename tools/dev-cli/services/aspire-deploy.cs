#region Purpose
// Targets, argument builders, preflight parsing and the operator text for `dev deploy` /
// `dev deprovision` (manual `aspire deploy` / `aspire destroy` per publish target).
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and gate
// both verbs without deploying anything. The process half (the aspire/helm/kubectl/az probes) lives in
// services/aspire-deploy-preflight.cs.
//
// Azure Container Apps (task 070-007): `aspire deploy` provisions with the Azure CLI credential, so
// the preflight requires `az login` (`az account show` succeeds). Aspire reads the subscription from
// the AppHost's configuration key Azure:SubscriptionId — the Azure__SubscriptionId environment
// variable, or the AppHost's user secrets. The az CLI subscription is a FALLBACK, never an override:
// the verb passes it to the aspire process as Azure__SubscriptionId only when neither of those is set
// (an injected env var would beat the user secret), and the plan prints which source chose the
// subscription. The user secret is read by wrapping the tool — `dotnet user-secrets list --project
// <apphost csproj>` — never by locating the secret store; the selection itself (ChooseAzureSubscription)
// is pure. A subscription Aspire remembered in its own deployment state is not consulted (the dev CLI
// never inspects that state; see below). Location and resource group stay Aspire's
// (Azure__Location / Azure__ResourceGroup, or its prompt). When `aspire destroy` has no record, the
// manual removal is `az group delete` (az asks for confirmation) plus purging the soft-deleted Key
// Vault: a deleted vault keeps its name reserved, and the Bicep derives the name from the resource
// group, so a redeploy into a same-named group fails until it is purged.
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

/// <summary>One deploy target: the AppHost's Publish:Target value.</summary>
internal sealed record DeployTarget(string Name);

/// <summary>The subscription the Azure CLI is logged in to (<c>az account show</c>).</summary>
internal sealed record AzureAccount(string Id, string Name);

/// <summary>The subscription an aca deploy targets, the line that says why, and whether the verb must pass it to Aspire.</summary>
internal sealed record AzureSubscriptionChoice(string SubscriptionId, string Detail, bool PassToAspire);

/// <summary>Builds and checks the <c>aspire deploy</c> / <c>aspire destroy</c> invocations for <c>dev deploy</c> / <c>dev deprovision</c>.</summary>
internal static class AspireDeploy
{
  internal static readonly Version MinimumCliVersion = new(13, 6);
  internal static readonly Version MinimumHelmVersion = new(4, 2);

  internal const string DeployEnvironment = "Production";
  internal const string ContainerRuntimeVariable = "ASPIRE_CONTAINER_RUNTIME";
  internal const string DefaultContainerRuntime = "docker";
  internal const string PostgresClaimName = "postgres-data";
  internal const string HelmReleaseNameParameter = "helm-release-name";
  internal const string KubernetesNamespaceParameter = "k8s-namespace";
  internal const string AzureSubscriptionIdVariable = "Azure__SubscriptionId";
  internal const string AzureSubscriptionIdConfigurationKey = "Azure:SubscriptionId";
  internal const string AzureResourceGroupVariable = "Azure__ResourceGroup";
  internal const string PostgresKeyVaultTag = "postgres-kv";

  internal static readonly DeployTarget Compose = new("compose");
  internal static readonly DeployTarget Kubernetes = new("kubernetes");
  internal static readonly DeployTarget ContainerApps = new("aca");

  internal static readonly DeployTarget[] Targets = [Compose, Kubernetes, ContainerApps];

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

  /// <summary><c>aspire destroy</c>; <c>--yes --non-interactive</c> only when the operator passed <c>--yes</c>, else Aspire asks.</summary>
  internal static string[] BuildDestroyArguments(string appHostProject, DeployTarget target, bool yes) =>
    yes
      ? ["destroy", "--apphost", appHostProject, "--environment", DeployEnvironment, "--yes", "--non-interactive", "--", $"--Publish:Target={target.Name}"]
      : ["destroy", "--apphost", appHostProject, "--environment", DeployEnvironment, "--", $"--Publish:Target={target.Name}"];

  internal static string[] BuildHelmVersionArguments() => ["version", "--short"];

  internal static string[] BuildKubectlContextArguments() => ["config", "current-context"];

  /// <summary><c>az account show</c>: the logged-in subscription id and name, one per line.</summary>
  internal static string[] BuildAzureAccountArguments() => ["account", "show", "--query", "[id, name]", "--output", "tsv"];

  /// <summary>The subscription from <c>az account show</c> output, or null when az failed (not logged in) or printed no id.</summary>
  internal static AzureAccount? ParseAzureAccount(bool probeSucceeded, string output)
  {
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return probeSucceeded && lines.Length > 0 && Guid.TryParse(lines[0], out _)
      ? new AzureAccount(lines[0], lines.Length > 1 ? lines[1] : "")
      : null;
  }

  internal const string NoAzureLoginMessage =
    "Not logged in to Azure (az is not on PATH or `az account show` failed). Run `az login` (and `az account set --subscription <id>` "
    + "to pick the subscription) first; `aspire deploy` provisions with the Azure CLI credential.";

  /// <summary><c>dotnet user-secrets list</c> for the AppHost: where an operator pins <c>Azure:SubscriptionId</c>.</summary>
  internal static string[] BuildUserSecretsListArguments(string appHostProject) =>
    ["user-secrets", "list", "--project", appHostProject];

  /// <summary>
  /// The value of <paramref name="key"/> (case-insensitive, like .NET configuration) in <c>dotnet user-secrets list</c>
  /// output (<c>Key = Value</c> lines), or null when the probe failed, the key is absent or its value is blank.
  /// </summary>
  internal static string? ParseUserSecret(bool probeSucceeded, string output, string key)
  {
    if (!probeSucceeded)
    {
      return null;
    }

    foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      int separator = line.IndexOf(" = ", StringComparison.Ordinal);
      if (separator > 0 && string.Equals(line[..separator].Trim(), key, StringComparison.OrdinalIgnoreCase))
      {
        string value = line[(separator + 3)..].Trim();
        return value.Length > 0 ? value : null;
      }
    }

    return null;
  }

  /// <summary>
  /// Subscription for the aca deploy, in the AppHost's own precedence: <c>Azure__SubscriptionId</c> when set, else the
  /// AppHost user secret <c>Azure:SubscriptionId</c> (both passed through untouched — Aspire reads them itself), else
  /// the <c>az account show</c> one, which only then is handed to <c>aspire deploy</c> as <c>Azure__SubscriptionId</c>.
  /// </summary>
  internal static AzureSubscriptionChoice ChooseAzureSubscription(string? environmentValue, string? userSecretValue, AzureAccount account)
  {
    if (!string.IsNullOrWhiteSpace(environmentValue))
    {
      return new AzureSubscriptionChoice(environmentValue.Trim(), $"Azure subscription: {environmentValue.Trim()} from the {AzureSubscriptionIdVariable} environment variable (az CLI is logged in to {account.Name})", PassToAspire: false);
    }

    if (!string.IsNullOrWhiteSpace(userSecretValue))
    {
      return new AzureSubscriptionChoice(userSecretValue.Trim(), $"Azure subscription: {userSecretValue.Trim()} from the AppHost user secret {AzureSubscriptionIdConfigurationKey} (az CLI is logged in to {account.Name})", PassToAspire: false);
    }

    return new AzureSubscriptionChoice(account.Id, $"Azure subscription: {account.Name} ({account.Id}) from `az account show` — neither {AzureSubscriptionIdVariable} nor the AppHost user secret {AzureSubscriptionIdConfigurationKey} is set", PassToAspire: true);
  }

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

  internal const string DestroyConfirmationRefusal =
    "Not destroying: no confirmation. Re-run with --yes to destroy non-interactively, or from a terminal to answer Aspire's prompt.";

  /// <summary>Printed after a failed <c>aspire destroy</c>: how to remove the deployment by hand.</summary>
  internal static string[] BuildManualCleanupLines(DeployTarget target, string containerRuntime) =>
  [
    "`aspire destroy` only knows deployments recorded on the machine (and checkout) that ran `aspire deploy`. If the stack is still running, remove it by hand — this deletes its data:",
    .. ManualRemovalLines(target, containerRuntime),
  ];

  private static string[] ManualRemovalLines(DeployTarget target, string containerRuntime) =>
    target == ContainerApps
      ?
      [
        $"  az group list --query \"[].name\" --output tsv                # find the resource group ({AzureResourceGroupVariable}, or the name aspire deploy asked for)",
        "  az group delete --name <resource-group>                      # az asks for confirmation; deletes every resource in the group",
        .. KeyVaultPurgeLines,
      ]
      : target == Kubernetes
      ?
      [
        "  helm list --all-namespaces                                  # find the release and namespace",
        $"  helm uninstall <release> --namespace <namespace>            # AppHost parameters {HelmReleaseNameParameter} and {KubernetesNamespaceParameter}",
        $"  kubectl delete pvc {PostgresClaimName} --namespace <namespace>",
      ]
      :
      [
        $"  {containerRuntime} compose ls                                     # find the project",
        $"  {containerRuntime} compose --project-name <project> down --volumes",
      ];

  // A deleted Key Vault is soft-deleted and keeps its name; the Bicep names it from the resource group.
  private static readonly string[] KeyVaultPurgeLines =
  [
    "Then purge the soft-deleted Key Vault, or a redeploy into a same-named resource group fails on the reserved vault name:",
    $"  az keyvault list-deleted --query \"[?properties.tags.\\\"aspire-resource-name\\\"=='{PostgresKeyVaultTag}'].name\" --output tsv",
    "  az keyvault purge --name <vault>",
  ];

  /// <summary>Kubernetes: the claim can outlive `helm uninstall`; aca: the Key Vault outlives the group. Tell the operator how to check.</summary>
  internal static string[] BuildPostDestroyLines(DeployTarget target) =>
    target == ContainerApps
      ? KeyVaultPurgeLines
      : target == Kubernetes
      ?
      [
        "Check that the postgres claim is gone: kubectl get pvc --namespace <namespace>",
        $"If {PostgresClaimName} remains, delete it: kubectl delete pvc {PostgresClaimName} --namespace <namespace>",
      ]
      : [];
}
