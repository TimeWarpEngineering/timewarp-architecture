#region Purpose
// The Azure Container Apps half of `dev deploy migrate` / `dev open`: resource group, Key Vault
// secret, temporary firewall rule and the EF bundle run.
#endregion

#region Design
// ACA: the bundle runs with --connection through a temporary `operator-migrate` firewall rule for the
// operator's IP; RunAcaBundleAsync creates the rule inside try and deletes it in finally, so it is
// deleted on a failed create, a failed bundle and an exception alike, and a failed delete is reported
// with the exact command to run by hand — from inside the finally, so even when an exception (Ctrl+C)
// propagates. The connection string is the Key Vault secret Aspire
// provisioned (connectionstrings--postgres-db in the postgres-kv vault); it is passed to the bundle
// only and never printed (RedactConnection). It is an argument because --connection is the bundle's
// documented interface; it is visible in the operator's own process list for the bundle's lifetime,
// accepted for an operator-run verb on the operator's machine. The operator's IP is --client-ip, else what
// https://api.ipify.org reports.
#endregion

namespace DevCli.Services;

using System.Net;

/// <summary>The Azure Container Apps half of <c>dev deploy migrate</c> / <c>dev open</c>: resource group, Key Vault secret, firewall rule and bundle.</summary>
internal static class DeployAca
{
  internal const string FirewallRuleName = "operator-migrate";
  internal const string ConnectionStringSecret = "connectionstrings--postgres-db";
  internal const string AzureResourceGroupConfigurationKey = "Azure:ResourceGroup";
  internal static readonly Uri ClientIpProbe = new("https://api.ipify.org");

  /// <summary>
  /// The resource group, in the order Aspire reads it plus the verbs' option: <c>--resource-group</c>, else
  /// <c>Azure__ResourceGroup</c>, else the AppHost user secret <c>Azure:ResourceGroup</c>.
  /// </summary>
  internal static ResourceGroupChoice ChooseResourceGroup(string? option, string? environmentValue, string? userSecretValue) =>
    !string.IsNullOrWhiteSpace(option) ? new ResourceGroupChoice(option.Trim(), $"Resource group: {option.Trim()} from --resource-group")
    : !string.IsNullOrWhiteSpace(environmentValue) ? new ResourceGroupChoice(environmentValue.Trim(), $"Resource group: {environmentValue.Trim()} from the {AspireDeployAzure.AzureResourceGroupVariable} environment variable")
    : !string.IsNullOrWhiteSpace(userSecretValue) ? new ResourceGroupChoice(userSecretValue.Trim(), $"Resource group: {userSecretValue.Trim()} from the AppHost user secret {AzureResourceGroupConfigurationKey}")
    : new ResourceGroupChoice(null, "");

  /// <summary>Refusal when no resource group is known, with the pwsh ways to name it.</summary>
  internal static string NoResourceGroupMessage(string verb, string appHostProject) =>
    $"No resource group for aca: pass it ({verb} --target aca --resource-group <name>), or set what `dev deploy --target aca` uses — "
    + $"${{env:{AspireDeployAzure.AzureResourceGroupVariable}}} = '<name>' for this pwsh session, or "
    + $"dotnet user-secrets set '{AzureResourceGroupConfigurationKey}' '<name>' --project '{appHostProject}'. "
    + "List them with: az group list --query \"[].name\" --output tsv";

  /// <summary><c>az containerapp show</c> of the ingress container app: its public FQDN.</summary>
  internal static string[] BuildIngressFqdnArguments(string resourceGroup, string subscriptionId) =>
  [
    "containerapp", "show", "--name", DeployOperate.IngressResourceName, "--resource-group", resourceGroup, "--subscription", subscriptionId,
    "--query", "properties.configuration.ingress.fqdn", "--output", "tsv",
  ];

  /// <summary>The single trimmed value an <c>--output tsv</c> query printed; null when none (or the call failed).</summary>
  internal static string? ParseSingleValue(bool succeeded, string output)
  {
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return succeeded && lines.Length == 1 ? lines[0] : null;
  }

  internal static string[] BuildFlexibleServerListArguments(string resourceGroup, string subscriptionId) =>
    ["postgres", "flexible-server", "list", "--resource-group", resourceGroup, "--subscription", subscriptionId, "--query", "[].name", "--output", "tsv"];

  internal static string[] BuildKeyVaultListArguments(string resourceGroup, string subscriptionId) =>
  [
    "keyvault", "list", "--resource-group", resourceGroup, "--subscription", subscriptionId,
    "--query", $"[?tags.\"aspire-resource-name\"=='{AspireDeploy.PostgresKeyVaultTag}'].name", "--output", "tsv",
  ];

  internal static string[] BuildConnectionStringArguments(string vault, string subscriptionId) =>
    ["keyvault", "secret", "show", "--vault-name", vault, "--name", ConnectionStringSecret, "--subscription", subscriptionId, "--query", "value", "--output", "tsv"];

  internal static string[] BuildFirewallRuleCreateArguments(string resourceGroup, string server, string subscriptionId, string clientIp) =>
  [
    "postgres", "flexible-server", "firewall-rule", "create", "--resource-group", resourceGroup, "--name", server,
    "--subscription", subscriptionId, "--rule-name", FirewallRuleName, "--start-ip-address", clientIp, "--end-ip-address", clientIp,
  ];

  internal static string[] BuildFirewallRuleDeleteArguments(string resourceGroup, string server, string subscriptionId) =>
  [
    "postgres", "flexible-server", "firewall-rule", "delete", "--resource-group", resourceGroup, "--name", server,
    "--subscription", subscriptionId, "--rule-name", FirewallRuleName, "--yes",
  ];

  /// <summary>The bundle's arguments; <paramref name="verbose"/> adds the bundle's own <c>--verbose</c>.</summary>
  internal static string[] BuildBundleArguments(string connectionString, bool verbose = false) =>
    verbose ? ["--connection", connectionString, "--verbose"] : ["--connection", connectionString];

  /// <summary>Arguments with every <c>--connection</c> value replaced, for printing.</summary>
  internal const string KeyVaultConnectionPlaceholder = "<connection string from Key Vault>";

  /// <summary>Printed in place of the direct Neon connection string.</summary>
  internal const string NeonDirectConnectionPlaceholder = "<direct Neon connection string>";

  /// <summary>Arguments with every <c>--connection</c> value replaced, for printing.</summary>
  internal static string[] RedactConnection(IReadOnlyList<string> arguments, string placeholder = KeyVaultConnectionPlaceholder) =>
    [.. arguments.Select((argument, index) => index > 0 && arguments[index - 1] == "--connection" ? placeholder : argument)];

  internal static string NoFlexibleServerMessage(string resourceGroup, int count) =>
    count == 0
      ? $"No PostgreSQL Flexible Server in resource group {resourceGroup}. Deploy first (dev deploy --target aca), or name the right group with --resource-group."
      : $"{count} PostgreSQL Flexible Servers in resource group {resourceGroup}; the aca deployment provisions one. Check the group: "
        + $"az postgres flexible-server list --resource-group {resourceGroup} --output table";

  /// <summary>Refusal when the Key Vault secret cannot be read: grant yourself the role first (pwsh), the verb never does.</summary>
  internal static string ConnectionStringUnreadableMessage(string resourceGroup, string? vault) =>
    (vault is null
      ? $"No Key Vault tagged aspire-resource-name={AspireDeploy.PostgresKeyVaultTag} in resource group {resourceGroup}. "
      : $"Cannot read the Key Vault secret {ConnectionStringSecret} in {vault}. ")
    + "The vault uses RBAC and the deployment grants you no role; grant yourself Key Vault Secrets User once (pwsh), then re-run:\n"
    + $"  $vault = az keyvault list --resource-group {resourceGroup} --query \"[?tags.`\"aspire-resource-name`\"=='{AspireDeploy.PostgresKeyVaultTag}'].name\" --output tsv\n"
    + "  az role assignment create --assignee (az ad signed-in-user show --query id --output tsv) --role 'Key Vault Secrets User' "
    + "--scope (az keyvault show --name $vault --query id --output tsv)";

  /// <summary>The operator's IP: <c>--client-ip</c> when it parses, else the probe's answer; null when neither is an IP.</summary>
  internal static string? ChooseClientIp(string? option, string? probed)
  {
    string? candidate = string.IsNullOrWhiteSpace(option) ? probed?.Trim() : option.Trim();
    return candidate is not null && IPAddress.TryParse(candidate, out IPAddress? address)
      && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
      ? address.ToString()
      : null;
  }

  internal static string NoClientIpMessage(string? option) =>
    string.IsNullOrWhiteSpace(option)
      ? $"Could not determine your public IPv4 address ({ClientIpProbe} did not answer with one). Pass it: dev deploy migrate --target aca --client-ip <ipv4>"
      : $"--client-ip '{option}' is not an IPv4 address (Flexible Server firewall rules take IPv4).";

  /// <summary>
  /// Creates the <c>operator-migrate</c> firewall rule, runs the bundle, and ALWAYS deletes the rule (finally) — on a failed
  /// create, a failed bundle and an exception alike. The exit code is the first failure's (create, then bundle), else the
  /// delete's; <paramref name="run"/> executes one step and returns its exit code. A failed delete is passed to
  /// <paramref name="reportCleanupFailure"/> inside the finally, so it is reported even when an exception propagates.
  /// </summary>
  internal static async Task<AcaMigrationResult> RunAcaBundleAsync(
    ProcessStep createRule, ProcessStep bundle, ProcessStep deleteRule,
    Func<ProcessStep, CancellationToken, Task<int>> run, Action<string> reportCleanupFailure, CancellationToken cancellationToken)
  {
    int exitCode = 0;
    int cleanupExitCode = 0;
    string? cleanupFailure = null;
    try
    {
      exitCode = await run(createRule, cancellationToken).ConfigureAwait(false);
      if (exitCode == 0)
      {
        exitCode = await run(bundle, cancellationToken).ConfigureAwait(false);
      }
    }
    finally
    {
      try
      {
        // Not the caller's token: Ctrl+C during the bundle must still remove the rule.
        cleanupExitCode = await run(deleteRule, CancellationToken.None).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is not OutOfMemoryException)
      {
        cleanupExitCode = -1;
        cleanupFailure = exception.Message;
      }

      if (cleanupExitCode != 0)
      {
        cleanupFailure ??= $"exit {cleanupExitCode}";
        // Reported here, not from the result: an exception from create/bundle skips the return.
        reportCleanupFailure(cleanupFailure);
      }
    }

    return new AcaMigrationResult(exitCode != 0 ? exitCode : cleanupExitCode == 0 ? 0 : 1, cleanupExitCode, cleanupFailure);
  }

  /// <summary>Printed when the firewall rule could not be deleted: the exact command (pwsh-safe) to remove it by hand.</summary>
  internal static string FirewallCleanupFailedMessage(string resourceGroup, string server, string subscriptionId, string reason) =>
    $"The temporary firewall rule {FirewallRuleName} could NOT be deleted ({reason}); it still admits your IP. Delete it now: "
    + $"az {string.Join(' ', BuildFirewallRuleDeleteArguments(resourceGroup, server, subscriptionId))}";
}
