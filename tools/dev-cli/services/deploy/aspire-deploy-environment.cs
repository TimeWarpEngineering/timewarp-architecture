#region Purpose
// Copies AppHost user secrets into the aspire process environment for `dev deploy`.
#endregion

#region Design
// Aspire reads its configuration from the environment, then (Development only) the AppHost user secrets.
// `aspire deploy --environment Production` does not load user secrets, so the values the operator keeps there
// are copied into the child process as Azure__* and Parameters__* variables. A variable already in the
// environment is inherited and left alone. Azure:Location and Azure:ResourceGroup user secrets are copied to
// Azure__Location and Azure__ResourceGroup when those variables are unset; otherwise Aspire prompts.
//
// A secret deploy value (a password, a client secret, a Neon connection string) is copied into the aspire
// process environment as Parameters__<name> (AddSecretParameterEnvironment). The plan and argv never contain it.
// The passkey host (Parameters:passkey-rp-host) is a hostname, not a secret; it is copied the same way.
#endregion

namespace DevCli.Services;

/// <summary>Copies AppHost user secrets into the aspire process environment, because Production does not load user secrets.</summary>
internal static class AspireDeployEnvironment
{
  /// <summary>
  /// Copies resolved secret values into <paramref name="aspireEnvironment"/> as <c>Parameters__&lt;name&gt;</c> when that
  /// variable is unset. Deploy runs as <see cref="AspireDeploy.DeployEnvironment"/>, which does not load AppHost user secrets.
  /// A variable already set is inherited and left alone. Nothing here is printed or placed on argv.
  /// </summary>
  internal static void AddSecretParameterEnvironment(
    IDictionary<string, string> aspireEnvironment,
    IReadOnlyList<ResolvedDeployParameter> secrets,
    Func<string, string?> environment)
  {
    foreach (ResolvedDeployParameter secret in secrets)
    {
      CopyWhenUnset(aspireEnvironment, environment, AspireDeployParameters.ParameterEnvironmentVariable(secret.Name), secret.Value);
    }
  }

  /// <summary>
  /// Copies <c>Parameters:passkey-rp-host</c> into <c>Parameters__passkey-rp-host</c> when that variable is unset.
  /// Production does not load user secrets. The value is a hostname, not a secret; it is still not printed here.
  /// </summary>
  internal static void CopyPasskeyHostWhenUnset(
    IDictionary<string, string> aspireEnvironment,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput)
  {
    CopyUserSecretWhenUnset(
      aspireEnvironment, environment, userSecretsProbeSucceeded, userSecretsOutput,
      AspireDeployParameters.ParameterEnvironmentVariable(AspireDeployParameters.PasskeyRpHostParameter), AspireDeployParameters.ParameterConfigurationKey(AspireDeployParameters.PasskeyRpHostParameter));
  }

  /// <summary>
  /// Copies Azure location, resource group and the Entra client secret from AppHost user secrets when the matching
  /// environment variable is unset. Same reason as <see cref="AddSecretParameterEnvironment"/>.
  /// </summary>
  internal static void AddAzureUserSecretEnvironment(
    IDictionary<string, string> aspireEnvironment,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput)
  {
    CopyUserSecretWhenUnset(aspireEnvironment, environment, userSecretsProbeSucceeded, userSecretsOutput, AspireDeployAzure.AzureLocationVariable, AspireDeployAzure.AzureLocationConfigurationKey);
    CopyUserSecretWhenUnset(aspireEnvironment, environment, userSecretsProbeSucceeded, userSecretsOutput, AspireDeployAzure.AzureResourceGroupVariable, AspireDeployAzure.AzureResourceGroupConfigurationKey);
    CopyUserSecretWhenUnset(
      aspireEnvironment, environment, userSecretsProbeSucceeded, userSecretsOutput,
      AspireDeployParameters.ParameterEnvironmentVariable(AspireDeployParameters.EntraClientSecretParameter), AspireDeployParameters.ParameterConfigurationKey(AspireDeployParameters.EntraClientSecretParameter));
  }

  private static void CopyUserSecretWhenUnset(
    IDictionary<string, string> aspireEnvironment,
    Func<string, string?> environment,
    bool userSecretsProbeSucceeded,
    string userSecretsOutput,
    string variable,
    string configurationKey)
  {
    string? value = AspireDeployParameters.ParseUserSecret(userSecretsProbeSucceeded, userSecretsOutput, configurationKey);
    if (value is not null)
    {
      CopyWhenUnset(aspireEnvironment, environment, variable, value);
    }
  }

  private static void CopyWhenUnset(
    IDictionary<string, string> aspireEnvironment, Func<string, string?> environment, string variable, string value)
  {
    if (string.IsNullOrWhiteSpace(environment(variable)))
    {
      aspireEnvironment[variable] = value;
    }
  }
}
