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
//
// Deploy parameters (tasks 286, 288): the AppHost declares per-deployment parameters with no value in
// code (for kubernetes: k8s-namespace, helm-release-name, registry-endpoint, registry-repository) and
// reads them from configuration as Parameters:<name>. None of them is a secret. Three are the app's
// identity — k8s-namespace, helm-release-name and registry-repository — and are committed in the
// AppHost appsettings.json Parameters section, set to the app's kebab name by the template
// (.template.config appNameKebab); registry-endpoint is committed as localhost:5001, the kind
// recipe's registry, and an AKS operator overrides it per machine. `dev deploy` resolves every
// required parameter of the target in the order Aspire's configuration applies it: a
// Parameters__<name> environment variable, then the AppHost user secret Parameters:<name> (read by
// wrapping `dotnet user-secrets list`), then appsettings.Production.json (DeployEnvironment — the
// template ships none) and appsettings.json beside the AppHost project, read as plain JSON (the
// Parameters section, keys matched case-insensitively), so the plan's "from …" label names the
// source Aspire will use. It refuses with one report listing each parameter still missing and the
// exact command to set it, and forwards the resolved values as `--Parameters:<name>=<value>` after
// `--`, so Aspire never reaches a parameter prompt it cannot show. The required lists are the
// AppHost's value-less parameters per target; a parameter with a default in code is not listed.
// Kubernetes preflight also requires the context's API
// to answer, and for a kind context (kind-<cluster>) that the kind cluster exists and the registry at
// registry-endpoint answers its /v2/ API (an HTTP probe, so still no container CLI). The environment
// variable is matched case-insensitively, like .NET configuration matches keys. When `dotnet user-secrets
// list` itself fails and a parameter is missing, the report says the secrets could not be read (with
// the tool's first error line) instead of only asking the operator to set values they may have set.
// Which cluster checks run and which refusals reach the report (CollectClusterProblems,
// CollectKubernetesProblems) is decided here, so it is tested; the process half only gathers probe
// results. Every probe has a timeout (ProbeTimeout): an exec credential plugin waiting for an
// interactive login or a wedged Docker daemon is reported as timed out, not waited on.
//
// Forwarded parameters must be non-secret: the plan prints each value and it is visible in the aspire
// process's argv. A secret deploy value (a password, a client secret) stays in the AppHost user secrets
// or a Parameters__<name> environment variable, where Aspire reads it itself; never add one to
// RequiredParameters, and never commit one to appsettings.json.
//
// `dev deprovision` resolves the same parameters best-effort and forwards those that are set, so
// `aspire destroy` sees the deployment's configuration if its pipeline resolves parameters; it never
// refuses on a missing one (destroy works from Aspire's recorded deployment state) and never probes
// the registry.
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>One deploy target: the AppHost's Publish:Target value.</summary>
internal sealed record DeployTarget(string Name);

/// <summary>The subscription the Azure CLI is logged in to (<c>az account show</c>).</summary>
internal sealed record AzureAccount(string Id, string Name);

/// <summary>The subscription an aca deploy targets, the line that says why, and whether the verb must pass it to Aspire.</summary>
internal sealed record AzureSubscriptionChoice(string SubscriptionId, string Detail, bool PassToAspire);

/// <summary>A deploy parameter value and where it came from (environment variable, AppHost user secret or appsettings file).</summary>
internal sealed record ResolvedDeployParameter(string Name, string Value, string Source);

/// <summary>An AppHost appsettings file (<see cref="FileName"/> beside the project) and its JSON text.</summary>
internal sealed record AppSettingsFile(string FileName, string Json);

/// <summary>The target's required parameters: those with a value, and the names nobody set.</summary>
internal sealed record DeployParameterResolution(IReadOnlyList<ResolvedDeployParameter> Resolved, IReadOnlyList<string> Missing);

/// <summary>
/// What the process half observed for one kubectl context: the API reachability probe, <c>kind get clusters</c> (kind contexts
/// only) and the registry probe (only when <see cref="AspireDeploy.RegistryToProbe"/> named an endpoint).
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
  internal const string RegistryEndpointParameter = "registry-endpoint";
  internal const string RegistryRepositoryParameter = "registry-repository";
  internal const string KindContextPrefix = "kind-";
  internal const string ReachabilityTimeout = "5s";
  internal const string UserSecretSource = "AppHost user secret";
  internal const string AppSettingsFileName = "appsettings.json";
  internal const string ParametersSection = "Parameters";

  /// <summary>Upper bound for each read-only preflight probe (kubectl, kind, helm, az, dotnet user-secrets).</summary>
  internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);
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

  /// <summary>
  /// <c>aspire deploy</c>; <c>--non-interactive</c> only when the operator passed <c>--yes</c>. The resolved deploy
  /// parameters follow the target after <c>--</c> as <c>--Parameters:&lt;name&gt;=&lt;value&gt;</c>.
  /// </summary>
  internal static string[] BuildDeployArguments(string appHostProject, DeployTarget target, bool nonInteractive, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    "deploy", "--apphost", appHostProject, "--environment", DeployEnvironment,
    .. nonInteractive ? (string[])["--non-interactive"] : [],
    "--", $"--Publish:Target={target.Name}",
    .. parameters.Select(parameter => $"--{ParameterConfigurationKey(parameter.Name)}={parameter.Value}"),
  ];

  /// <summary>The AppHost parameters a deploy of <paramref name="target"/> needs and that have no default.</summary>
  internal static string[] RequiredParameters(DeployTarget target) =>
    target == Kubernetes
      ? [KubernetesNamespaceParameter, HelmReleaseNameParameter, RegistryEndpointParameter, RegistryRepositoryParameter]
      : [];

  /// <summary>Configuration key of an AppHost parameter: <c>Parameters:&lt;name&gt;</c>.</summary>
  internal static string ParameterConfigurationKey(string name) => $"Parameters:{name}";

  /// <summary>Environment variable of an AppHost parameter: <c>Parameters__&lt;name&gt;</c>.</summary>
  internal static string ParameterEnvironmentVariable(string name) => $"Parameters__{name}";

  /// <summary>
  /// Environment lookup that matches names case-insensitively, like .NET configuration does (an exact match wins when
  /// two variables differ only by case).
  /// </summary>
  internal static Func<string, string?> CaseInsensitiveEnvironment(IReadOnlyDictionary<string, string> variables) =>
    name => variables.TryGetValue(name, out string? exact)
      ? exact
      : variables.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

  /// <summary>
  /// The AppHost appsettings files Aspire reads for a deploy, highest precedence first:
  /// <c>appsettings.&lt;DeployEnvironment&gt;.json</c>, then <c>appsettings.json</c>.
  /// </summary>
  internal static string[] AppSettingsFileNames() => [$"appsettings.{DeployEnvironment}.json", AppSettingsFileName];

  /// <summary>Source label of a value read from an AppHost appsettings file.</summary>
  internal static string AppSettingsSource(string fileName) => $"AppHost {fileName}";

  /// <summary>
  /// The value of <c>Parameters:&lt;name&gt;</c> in appsettings JSON (section and key matched case-insensitively, like .NET
  /// configuration), or null when the JSON is unparseable, the key is absent or its value is blank.
  /// </summary>
  internal static string? ParseAppSettingsParameter(string json, string name)
  {
    try
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
      if (document.RootElement.ValueKind != JsonValueKind.Object
        || FindProperty(document.RootElement, ParametersSection) is not { ValueKind: JsonValueKind.Object } section
        || FindProperty(section, name) is not { } value)
      {
        return null;
      }

      string? text = value.ValueKind switch
      {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
        _ => null,
      };
      return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static JsonElement? FindProperty(JsonElement element, string name)
  {
    JsonElement? found = null;
    foreach (JsonProperty property in element.EnumerateObject())
    {
      // Last one wins, like the JSON configuration provider.
      if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
      {
        found = property.Value;
      }
    }

    return found;
  }

  /// <summary>
  /// Resolves the target's required parameters in Aspire's configuration order: the <c>Parameters__&lt;name&gt;</c> environment
  /// variable, else the AppHost user secret <c>Parameters:&lt;name&gt;</c> (from <c>dotnet user-secrets list</c> output), else
  /// the first of <paramref name="appSettings"/> (highest precedence first) that sets it. Blank values count as missing.
  /// </summary>
  internal static DeployParameterResolution ResolveParameters(
    DeployTarget target,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput,
    IReadOnlyList<AppSettingsFile>? appSettings = null)
  {
    List<ResolvedDeployParameter> resolved = [];
    List<string> missing = [];
    foreach (string name in RequiredParameters(target))
    {
      string? fromEnvironment = environment(ParameterEnvironmentVariable(name));
      string? fromSecret = ParseUserSecret(userSecretsProbeSucceeded, userSecretsOutput, ParameterConfigurationKey(name));
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
      {
        resolved.Add(new ResolvedDeployParameter(name, fromEnvironment.Trim(), $"{ParameterEnvironmentVariable(name)} environment variable"));
      }
      else if (fromSecret is not null)
      {
        resolved.Add(new ResolvedDeployParameter(name, fromSecret, UserSecretSource));
      }
      else if ((appSettings ?? [])
        .Select(file => (file.FileName, Value: ParseAppSettingsParameter(file.Json, name)))
        .FirstOrDefault(candidate => candidate.Value is not null) is { Value: { } fromAppSettings } candidate)
      {
        resolved.Add(new ResolvedDeployParameter(name, fromAppSettings, AppSettingsSource(candidate.FileName)));
      }
      else
      {
        missing.Add(name);
      }
    }

    return new DeployParameterResolution(resolved, missing);
  }

  /// <summary>The refusal for unset deploy parameters: each one with the pwsh command that sets it.</summary>
  internal static string[] BuildMissingParameterLines(string appHostProject, DeployTarget target, IReadOnlyList<string> missing) =>
  [
    $"Missing deploy parameter{(missing.Count == 1 ? "" : "s")} for {target.Name}: {string.Join(", ", missing)}. "
      + $"Commit each in the {ParametersSection} section of the AppHost {AppSettingsFileName} when every machine shares it, or set it per machine in the AppHost user secrets:",
    .. missing.Select(name => $"  dotnet user-secrets set '{ParameterConfigurationKey(name)}' '<value>' --project '{appHostProject}'"),
    "or for the current pwsh session only:",
    .. missing.Select(name => $"  ${{env:{ParameterEnvironmentVariable(name)}}} = '<value>'"),
  ];

  /// <summary>
  /// Deploy refusal lines for unset parameters: none when all are set; else, when the user secrets could not be read
  /// (<paramref name="userSecretsFailure"/>), that first, then the missing list with the command to set each.
  /// </summary>
  internal static string[] CollectParameterProblems(
    string appHostProject, DeployTarget target, DeployParameterResolution parameters, string? userSecretsFailure) =>
    parameters.Missing.Count == 0
      ? []
      :
      [
        .. userSecretsFailure is null ? (string[])[] : [UserSecretsUnreadableMessage(appHostProject, userSecretsFailure)],
        string.Join('\n', BuildMissingParameterLines(appHostProject, target, parameters.Missing)),
      ];

  /// <summary>The AppHost user secrets could not be listed, so a "missing" parameter may in fact be set.</summary>
  internal static string UserSecretsUnreadableMessage(string appHostProject, string reason) =>
    $"Could not read the AppHost user secrets (`dotnet user-secrets list --project '{appHostProject}'` failed: {reason}), "
    + "so parameters set there are not visible. Fix that first (e.g. `dotnet restore` the AppHost); the list below may already be set.";

  /// <summary>First non-blank line of <paramref name="text"/>, else <paramref name="fallback"/>.</summary>
  internal static string FirstLine(string text, string fallback) =>
    text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fallback;

  /// <summary>Refusal for a probe that hit <see cref="ProbeTimeout"/>.</summary>
  internal static string ProbeTimedOutMessage(string executable, IReadOnlyList<string> arguments) =>
    $"`{executable} {string.Join(' ', arguments)}` timed out after {ProbeTimeout.TotalSeconds:0}s (an exec credential plugin waiting for "
    + "an interactive login, or a wedged Docker daemon?). Make it answer from this shell, then re-run.";

  /// <summary>
  /// <c>aspire destroy</c>; <c>--yes --non-interactive</c> only when the operator passed <c>--yes</c>, else Aspire asks. Any
  /// deploy parameter the operator has set follows the target as <c>--Parameters:&lt;name&gt;=&lt;value&gt;</c>.
  /// </summary>
  internal static string[] BuildDestroyArguments(string appHostProject, DeployTarget target, bool yes, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    "destroy", "--apphost", appHostProject, "--environment", DeployEnvironment,
    .. yes ? (string[])["--yes", "--non-interactive"] : [],
    "--", $"--Publish:Target={target.Name}",
    .. parameters.Select(parameter => $"--{ParameterConfigurationKey(parameter.Name)}={parameter.Value}"),
  ];

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
      : $"The container registry at {endpoint} ({RegistryEndpointParameter}) does not answer{(string.IsNullOrWhiteSpace(failure) ? "" : $" ({failure.Trim()})")}. "
        + "For kind, start the local registry container (kind-registry in the tw-deploy skill's kind recipe), or point the parameter at a running registry: "
        + $"dotnet user-secrets set '{ParameterConfigurationKey(RegistryEndpointParameter)}' '<host:port>' --project '{appHostProject}'";

  /// <summary>
  /// The registry to probe for <paramref name="context"/>: <c>registry-endpoint</c> when deploying (<paramref name="deploying"/>)
  /// to a kind context and the parameter resolved; null otherwise (deprovision, a non-kind context, or the parameter is unset).
  /// </summary>
  internal static string? RegistryToProbe(string context, DeployParameterResolution parameters, bool deploying) =>
    deploying && KindClusterName(context) is not null
      ? parameters.Resolved.FirstOrDefault(parameter => parameter.Name == RegistryEndpointParameter)?.Value
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

  /// <summary>One report for every preflight problem found, so the operator fixes them in one pass.</summary>
  internal static string[] BuildPreflightReport(string verb, IReadOnlyList<string> problems) =>
  [
    $"{verb} preflight failed ({problems.Count} problem{(problems.Count == 1 ? "" : "s")}); nothing was run:",
    .. problems.Select(problem => $"- {problem}"),
  ];

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
  internal static string[] BuildDeployPlanLines(string appHostProject, DeployTarget target, string preflightDetail, IReadOnlyList<ResolvedDeployParameter> parameters) =>
  [
    $"dev deploy → aspire deploy ({target.Name})",
    $"  AppHost:     {appHostProject}",
    $"  Environment: {DeployEnvironment}",
    $"  Target:      Publish:Target={target.Name}",
    .. BuildParameterLines(parameters),
    $"  {preflightDetail}",
  ];

  /// <summary>One line per forwarded parameter: name, value and source (forwarded parameters are never secret).</summary>
  internal static string[] BuildParameterLines(IReadOnlyList<ResolvedDeployParameter> parameters) =>
    [.. parameters.Select(parameter => $"  Parameter:   {parameter.Name}={parameter.Value} (from the {parameter.Source})")];

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
