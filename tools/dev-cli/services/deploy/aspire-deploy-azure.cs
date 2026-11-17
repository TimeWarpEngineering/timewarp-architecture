#region Purpose
// The Azure account and subscription half of the aca / aca-neon preflight: `az account show`
// parsing and the subscription choice.
#endregion

#region Design
// Azure Container Apps (task 070-007): `aspire deploy` provisions with the Azure CLI credential, so
// the preflight requires `az login` (`az account show` succeeds). Aspire reads the subscription from
// the AppHost's configuration key Azure:SubscriptionId — the Azure__SubscriptionId environment
// variable, or the AppHost's user secrets. `aspire deploy --environment Production` does not load
// user secrets (they load only when the host environment is Development), so a user secret is copied
// into the child process as Azure__SubscriptionId. An Azure__SubscriptionId already in the environment
// is inherited and not rewritten. The az CLI subscription is the fallback, copied the same way, and
// never overrides a subscription the operator pinned. The plan prints which source chose it. The user
// secret is read by wrapping `dotnet user-secrets list --project <apphost csproj>`, never by locating
// the secret store; ChooseAzureSubscription is pure. A subscription Aspire remembered in its own
// deployment state is not consulted (the dev CLI never inspects that state).
#endregion

namespace DevCli.Services;

/// <summary>The Azure account and subscription half of the aca preflight: <c>az account show</c> parsing and the subscription choice.</summary>
internal static class AspireDeployAzure
{
  internal const string AzureSubscriptionIdVariable = "Azure__SubscriptionId";
  internal const string AzureSubscriptionIdConfigurationKey = "Azure:SubscriptionId";
  internal const string AzureResourceGroupVariable = "Azure__ResourceGroup";
  internal const string AzureResourceGroupConfigurationKey = "Azure:ResourceGroup";
  internal const string AzureLocationVariable = "Azure__Location";
  internal const string AzureLocationConfigurationKey = "Azure:Location";

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

  /// <summary>
  /// Subscription for the aca deploy, in the AppHost's own precedence: <c>Azure__SubscriptionId</c> when set, else the
  /// AppHost user secret <c>Azure:SubscriptionId</c>, else the <c>az account show</c> one. <see cref="AzureSubscriptionChoice.PassToAspire"/>
  /// is true when the child must receive <c>Azure__SubscriptionId</c>: Production does not load user secrets, and the
  /// az CLI value is not already in the environment. An environment value is inherited (<c>PassToAspire</c> false).
  /// </summary>
  internal static AzureSubscriptionChoice ChooseAzureSubscription(string? environmentValue, string? userSecretValue, AzureAccount account)
  {
    if (!string.IsNullOrWhiteSpace(environmentValue))
    {
      return new AzureSubscriptionChoice(environmentValue.Trim(), $"Azure subscription: {environmentValue.Trim()} from the {AzureSubscriptionIdVariable} environment variable (az CLI is logged in to {account.Name})", PassToAspire: false);
    }

    if (!string.IsNullOrWhiteSpace(userSecretValue))
    {
      return new AzureSubscriptionChoice(
        userSecretValue.Trim(),
        $"Azure subscription: {userSecretValue.Trim()} from the AppHost user secret {AzureSubscriptionIdConfigurationKey} (copied to {AzureSubscriptionIdVariable}; aspire deploy runs as {AspireDeploy.DeployEnvironment} and does not load user secrets; az CLI is logged in to {account.Name})",
        PassToAspire: true);
    }

    return new AzureSubscriptionChoice(account.Id, $"Azure subscription: {account.Name} ({account.Id}) from `az account show` — neither {AzureSubscriptionIdVariable} nor the AppHost user secret {AzureSubscriptionIdConfigurationKey} is set", PassToAspire: true);
  }
}
