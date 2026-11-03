#region Purpose
// `dev entra setup` step: mint the client secret and write Web.Server user secrets.
#endregion

#region Design
// Mint a client secret on first write, --new-secret, or when stored ClientId/TenantId differs
// from the target app (GUID-equal, case-insensitive). A failed user-secrets list aborts
// (does not fail-open mint). The password is never printed.
// SignInOptions:Methods is appended, not replaced: Microsoft365 is written at the next index only
// when the checked-in list plus user secrets do not already contain it.
#endregion

namespace DevCli.Services;

internal static class EntraSetupSecretsStep
{
  internal static async Task<bool> MaybeMintSecretAsync(EntraSetupContext context)
  {
    Dictionary<string, string> secrets = [];
    bool listSucceeded = true;
    CommandOutput? listOutput = null;
    if (!context.Options.NewSecret)
    {
      listOutput = await context.Cli.ListUserSecretsAsync().ConfigureAwait(false);
      listSucceeded = listOutput.Success;
      secrets = listSucceeded ? EntraUserSecrets.ParseUserSecretsList(listOutput.Stdout) : [];
      if (listSucceeded)
      {
        context.ListedSecrets = secrets;
        context.ListedSecretsCaptured = true;
      }
    }

    secrets.TryGetValue(EntraSetup.ClientIdKey, out string? existingClientId);
    secrets.TryGetValue(EntraSetup.TenantIdKey, out string? existingTenantId);
    secrets.TryGetValue(EntraSetup.TenantDisplayNameKey, out string? existingTenantDisplayName);

    if (!EntraClientSecrets.TryDecideMintClientSecret(
      context.Options.NewSecret,
      listSucceeded,
      EntraUserSecrets.HasClientSecret(secrets),
      existingClientId,
      existingTenantId,
      context.AppId,
      context.TenantId,
      out bool mint,
      out bool appRegistrationChanged))
    {
      context.Cli.WriteFailure(
        listOutput ?? CommandOutput.Empty(),
        "Failed to list Web.Server user secrets; not minting a client secret.");
      Environment.ExitCode = 1;
      return false;
    }

    context.MintedBecauseAppChanged = appRegistrationChanged;
    if (appRegistrationChanged)
    {
      string oldTenantLabel = !string.IsNullOrWhiteSpace(existingTenantDisplayName)
        ? existingTenantDisplayName
        : existingTenantId ?? "";
      context.Terminal.WriteLine(
        EntraClientSecrets.FormatAppRegistrationChangedMessage(existingClientId, oldTenantLabel));
    }

    if (context.Cli.IsDryRun)
    {
      context.Terminal.WriteLine(
        EntraClientSecrets.FormatMintDryRunDecision(mint, context.Options.NewSecret, appRegistrationChanged));
    }

    if (!mint)
    {
      return true;
    }

    string displayName = EntraSetup.CredentialDisplayName(DateTimeOffset.UtcNow);
    string[] arguments =
    [
      "ad",
      "app",
      "credential",
      "reset",
      "--id",
      context.AppId,
      "--append",
      "--display-name",
      displayName,
      "--years",
      "1",
      "--query",
      "password",
      "-o",
      "tsv"
    ];
    CommandOutput output = await context.Cli.CaptureAzSecretAsync(arguments).ConfigureAwait(false);
    if (context.Cli.IsDryRun)
    {
      context.MintedSecret = EntraSetup.MaskedSecret;
      return true;
    }

    if (!output.Success)
    {
      context.Cli.WriteFailure(output, "Failed to mint an app credential. Stdout (the password) was not printed.");
      Environment.ExitCode = 1;
      return false;
    }

    context.MintedSecret = output.Stdout.Trim();
    if (string.IsNullOrEmpty(context.MintedSecret))
    {
      context.Terminal.WriteErrorLine("az ad app credential reset returned an empty password.".Red());
      Environment.ExitCode = 1;
      return false;
    }

    context.Terminal.WriteLine($"Minted client secret `{displayName}` (value not printed).");
    return true;
  }

  internal static async Task<bool> WriteUserSecretsAsync(EntraSetupContext context)
  {
    if (!context.ListedSecretsCaptured)
    {
      CommandOutput listed = await context.Cli.ListUserSecretsAsync().ConfigureAwait(false);
      if (!listed.Success)
      {
        context.Cli.WriteFailure(listed, "Failed to list Web.Server user secrets.");
        Environment.ExitCode = 1;
        return false;
      }

      context.ListedSecrets = EntraUserSecrets.ParseUserSecretsList(listed.Stdout);
      context.ListedSecretsCaptured = true;
    }

    IReadOnlyList<string> checkedIn = EntraUserSecrets.ReadCheckedInSignInMethods(
      Path.Combine(context.RepoRoot, EntraSetup.WebServerAppSettings));
    string? methodKey = EntraUserSecrets.Microsoft365MethodKeyToAdd(checkedIn, context.ListedSecrets);
    List<(string Key, string Value, bool Mask)> pairs =
    [
      (EntraSetup.TenantIdKey, context.TenantId, false),
      (EntraSetup.ClientIdKey, context.AppId, false),
      (EntraSetup.AllowBootstrapKey, "true", false)
    ];

    if (methodKey is not null)
    {
      pairs.Insert(0, (methodKey, EntraSetup.Microsoft365MethodName, false));
    }

    if (!string.IsNullOrWhiteSpace(context.TenantDisplayName))
    {
      pairs.Add((EntraSetup.TenantDisplayNameKey, context.TenantDisplayName, false));
    }

    if (!string.IsNullOrWhiteSpace(context.TenantDomain))
    {
      pairs.Add((EntraSetup.TenantDomainKey, context.TenantDomain, false));
    }

    if (context.MintedSecret is not null)
    {
      pairs.Add((EntraSetup.ClientSecretKey, context.MintedSecret, true));
    }

    if (!string.IsNullOrWhiteSpace(context.Options.PublicOrigin))
    {
      pairs.Add((EntraSetup.PublicOriginKey, context.Options.PublicOrigin.Trim().TrimEnd('/'), false));
    }

    foreach ((string key, string value, bool mask) in pairs)
    {
      CommandOutput output = await context.Cli.SetUserSecretAsync(key, value, mask).ConfigureAwait(false);
      if (!output.Success)
      {
        context.Cli.WriteFailure(output, $"Failed to write user secret {key}.");
        Environment.ExitCode = 1;
        return false;
      }

      context.WrittenSecretKeys.Add(mask ? $"{key}={EntraSetup.MaskedSecret}" : key);
    }

    return true;
  }
}
