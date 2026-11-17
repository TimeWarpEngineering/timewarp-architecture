#region Purpose
// Resolves the secret deploy parameters (aca-neon's Neon connection strings) and words the
// refusals for missing or committed ones.
#endregion

#region Design
// aca-neon (task 317) is an Azure Container Apps target with no Flexible Server. Its two Neon
// connection strings are SecretParameters: resolved from the environment or AppHost user secrets,
// refused when a copy is in appsettings, and checked for shape. dev deploy resolves both and copies
// them into the aspire environment as AspireDeployEnvironment does. dev deploy migrate resolves and shape-checks only the
// direct string and does not probe az login. Neither value is printed or placed on argv.
// Destroying the resource group does not delete the Neon project and there is no Key Vault to purge.
//
// A secret is never printed, never placed on argv and never suggested for appsettings.json; the refusals here
// name the parameter, not its value.
#endregion

namespace DevCli.Services;

/// <summary>Resolves the secret deploy parameters (the aca-neon connection strings) and words their refusals. Never carries a value into text.</summary>
internal static class AspireDeploySecrets
{
  internal const string NeonPooledConnectionStringParameter = "neon-pooled-connection-string";
  internal const string NeonDirectConnectionStringParameter = "neon-direct-connection-string";

  /// <summary>Secret parameters Aspire reads itself. Never forwarded and never printed.</summary>
  internal static string[] SecretParameters(DeployTarget target) =>
    SecretParameters(target, PreflightScope.Deploy);

  /// <summary>
  /// Secret parameters for one verb. aca-neon migrate needs only the direct string. Deploy needs both.
  /// </summary>
  internal static string[] SecretParameters(DeployTarget target, PreflightScope scope)
  {
    if (target != AspireDeploy.ContainerAppsNeon)
    {
      return [];
    }

    return scope == PreflightScope.Migrate
      ? [NeonDirectConnectionStringParameter]
      : [NeonPooledConnectionStringParameter, NeonDirectConnectionStringParameter];
  }

  /// <summary>
  /// Resolves secret parameters from the environment, then user secrets. A value in appsettings is reported and not used.
  /// </summary>
  internal static SecretResolution ResolveSecrets(
    IReadOnlyList<string> names,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput,
    IReadOnlyList<AppSettingsFile>? appSettings = null)
  {
    List<ResolvedDeployParameter> resolved = [];
    List<string> missing = [];
    List<string> committed = [];
    foreach (string name in names)
    {
      string? fromAppSettings = (appSettings ?? [])
        .Select(file => AspireDeployParameters.ParseAppSettingsParameter(file.Json, name))
        .FirstOrDefault(value => value is not null);
      if (fromAppSettings is not null)
      {
        committed.Add(name);
      }

      string? fromEnvironment = environment(AspireDeployParameters.ParameterEnvironmentVariable(name));
      string? fromSecret = AspireDeployParameters.ParseUserSecret(userSecretsProbeSucceeded, userSecretsOutput, AspireDeployParameters.ParameterConfigurationKey(name));
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
      {
        resolved.Add(new ResolvedDeployParameter(name, fromEnvironment.Trim(), $"{AspireDeployParameters.ParameterEnvironmentVariable(name)} environment variable"));
      }
      else if (fromSecret is not null)
      {
        resolved.Add(new ResolvedDeployParameter(name, fromSecret, AspireDeployParameters.UserSecretSource));
      }
      else if (fromAppSettings is null)
      {
        missing.Add(name);
      }
    }

    return new SecretResolution(resolved, missing, committed);
  }

  /// <summary>Refusal lines for secret parameters. Never includes a secret value and never suggests appsettings.</summary>
  internal static string[] CollectSecretProblems(
    string appHostProject, DeployTarget target, SecretResolution resolution, string? userSecretsFailure)
  {
    List<string> lines = [];
    if (userSecretsFailure is not null && resolution.Missing.Count > 0)
    {
      lines.Add(AspireDeployParameters.UserSecretsUnreadableMessage(appHostProject, userSecretsFailure));
    }

    foreach (string name in resolution.CommittedToAppSettings)
    {
      lines.Add(
        $"Neon connection string '{name}' is in the AppHost appsettings. Remove it and set it with user secrets; a connection string must never be committed.");
    }

    if (resolution.Missing.Count > 0)
    {
      lines.Add(
        $"Missing Neon connection string{(resolution.Missing.Count == 1 ? "" : "s")} for {target.Name}: {string.Join(", ", resolution.Missing)}. "
        + "Set each in the AppHost user secrets or a Parameters__ environment variable. Do not commit them in appsettings.json.");
      foreach (string name in resolution.Missing)
      {
        lines.Add($"  dotnet user-secrets set '{AspireDeployParameters.ParameterConfigurationKey(name)}' '<value>' --project '{appHostProject}'");
      }
    }

    return [.. lines];
  }
}
