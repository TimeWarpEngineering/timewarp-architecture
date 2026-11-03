#region Purpose
// Resolves the AppHost deploy parameters for `dev deploy` / `dev deprovision` / `dev open` /
// `dev deploy migrate`: environment, user secret, then appsettings, plus the refusals for unset ones.
#endregion

#region Design
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
//
// The environment variable is matched case-insensitively, like .NET configuration matches keys. When `dotnet user-secrets
// list` itself fails and a parameter is missing, the report says the secrets could not be read (with
// the tool's first error line) instead of only asking the operator to set values they may have set.
//
// Forwarded parameters must be non-secret: the plan prints each value and it is visible in the aspire
// process's argv. A secret deploy value (a password, a client secret) stays in the AppHost user secrets
// or a Parameters__<name> environment variable. Never add one to RequiredParameters, and never commit
// one to appsettings.json. Deploy runs as Production, so a secret resolved from user secrets is copied
// into the aspire process environment as Parameters__<name> (AddSecretParameterEnvironment). A value
// already in the environment is left alone. The plan and argv never contain it.
//
// `dev deprovision` resolves the same parameters best-effort and forwards those that are set, so
// `aspire destroy` sees the deployment's configuration if its pipeline resolves parameters; it never
// refuses on a missing one (destroy works from Aspire's recorded deployment state) and never probes
// the registry.
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>Resolves the AppHost deploy parameters (environment, user secrets, appsettings) and words the refusals for missing ones.</summary>
internal static class AspireDeployParameters
{
  internal const string HelmReleaseNameParameter = "helm-release-name";
  internal const string KubernetesNamespaceParameter = "k8s-namespace";
  internal const string RegistryEndpointParameter = "registry-endpoint";
  internal const string RegistryRepositoryParameter = "registry-repository";
  internal const string UserSecretSource = "AppHost user secret";
  internal const string AppSettingsFileName = "appsettings.json";
  internal const string ParametersSection = "Parameters";
  internal const string EntraClientSecretParameter = "entra-client-secret";

  /// <summary>Optional extra passkey RP host (a custom domain or a non-localhost ingress). Not a secret.</summary>
  internal const string PasskeyRpHostParameter = "passkey-rp-host";

  /// <summary>The AppHost parameters a deploy of <paramref name="target"/> needs and that have no default.</summary>
  internal static string[] RequiredParameters(DeployTarget target) =>
    target == AspireDeploy.Kubernetes
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
  internal static string[] AppSettingsFileNames() => [$"appsettings.{AspireDeploy.DeployEnvironment}.json", AppSettingsFileName];

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
        // Objects, arrays, null, and undefined are absent parameters, same as the former discard.
        JsonValueKind.Undefined or JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null => null,
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
    IReadOnlyList<AppSettingsFile>? appSettings = null) =>
    ResolveParameters(RequiredParameters(target), environment, userSecretsProbeSucceeded, userSecretsOutput, appSettings);

  /// <summary>Resolves the named AppHost parameters in the same order as the target overload (used by `dev open` for ingress-port).</summary>
  internal static DeployParameterResolution ResolveParameters(
    IReadOnlyList<string> names,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput,
    IReadOnlyList<AppSettingsFile>? appSettings = null)
  {
    List<ResolvedDeployParameter> resolved = [];
    List<string> missing = [];
    foreach (string name in names)
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

  /// <summary>One line per forwarded parameter: name, value and source (forwarded parameters are never secret).</summary>
  internal static string[] BuildParameterLines(IReadOnlyList<ResolvedDeployParameter> parameters) =>
    [.. parameters.Select(parameter => $"  Parameter:   {parameter.Name}={parameter.Value} (from the {parameter.Source})")];
}
