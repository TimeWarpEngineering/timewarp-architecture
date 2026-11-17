#region Purpose
// `dev entra setup` step: find-or-create the app registration, merge redirect URIs, ensure the service principal.
#endregion

#region Design
// Idempotent by display name: prefer domain-suffixed default, reuse bare legacy name if present.
// Mutations (app create/update, sp) still skip on dry-run.
#endregion

namespace DevCli.Services;

internal static class EntraSetupAppStep
{
  internal static async Task<bool> FindOrCreateAppAsync(EntraSetupContext context)
  {
    IReadOnlyList<string> lookupNames = EntraAppLookup.AppLookupNames(context.Options.Name, context.TenantDomain);
    string listDisplayName = string.IsNullOrWhiteSpace(context.Options.Name)
      ? EntraSetup.DefaultDisplayName
      : context.Options.Name.Trim();
    string preferredName = lookupNames[0];
    string? legacyName = lookupNames.Count > 1 ? lookupNames[1] : null;

    string[] listArguments =
    [
      "ad",
      "app",
      "list",
      "--display-name",
      listDisplayName,
      "--query",
      "[].{appId:appId,displayName:displayName}",
      "-o",
      "json"
    ];
    CommandOutput listOutput = await context.Cli.CaptureAzReadOnlyAsync(listArguments).ConfigureAwait(false);
    List<string> createArguments =
    [
      "ad",
      "app",
      "create",
      "--display-name",
      context.DisplayName,
      "--sign-in-audience",
      "AzureADMyOrg",
      "--web-redirect-uris",
      .. context.DesiredRedirectUris,
      "--query",
      "appId",
      "-o",
      "tsv"
    ];
    if (!listOutput.Success)
    {
      context.Cli.WriteFailure(listOutput, "Failed to list Entra app registrations.");
      Environment.ExitCode = 1;
      return false;
    }

    if (!EntraAzJson.TryFindExactAppIds(listOutput.Stdout, preferredName, out IReadOnlyList<string> preferredIds))
    {
      context.Terminal.WriteErrorLine("Could not parse `az ad app list` JSON.".Red());
      Environment.ExitCode = 1;
      return false;
    }

    IReadOnlyList<string> legacyIds = [];
    if (legacyName is not null)
    {
      if (!EntraAzJson.TryFindExactAppIds(listOutput.Stdout, legacyName, out legacyIds))
      {
        context.Terminal.WriteErrorLine("Could not parse `az ad app list` JSON.".Red());
        Environment.ExitCode = 1;
        return false;
      }
    }

    EntraAppLookup.ResolveExistingApp(preferredIds, legacyIds, out string? existingAppId, out bool ambiguous);
    if (ambiguous)
    {
      context.Terminal.WriteErrorLine(
        $"Multiple app registrations named '{preferredName}'. Rename extras or pass --name.".Red());
      Environment.ExitCode = 1;
      return false;
    }

    if (existingAppId is not null)
    {
      context.AppId = existingAppId;
      string reusedName = preferredIds.Count == 1
        ? preferredName
        : legacyName ?? preferredName;
      context.Terminal.WriteLine($"Reusing app registration {context.AppId} ({reusedName}).");
      context.DisplayName = reusedName;
      return true;
    }

    if (context.Cli.IsDryRun)
    {
      context.AppId = "<appId>";
      context.Terminal.WriteLine("dry-run: would create the app registration.");
      await context.Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
      return true;
    }

    CommandOutput createOutput = await context.Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
    if (!createOutput.Success)
    {
      context.Cli.WriteFailure(createOutput, "Failed to create the Entra app registration.");
      Environment.ExitCode = 1;
      return false;
    }

    context.AppId = createOutput.Stdout.Trim();
    if (context.AppId.Length == 0)
    {
      context.Terminal.WriteErrorLine("az ad app create succeeded but returned no appId.".Red());
      Environment.ExitCode = 1;
      return false;
    }

    context.CreatedApp = true;
    context.Terminal.WriteLine($"Created app registration {context.AppId} ({context.DisplayName}).");
    return true;
  }

  internal static async Task<bool> MergeRedirectUrisAsync(EntraSetupContext context)
  {
    if (context.CreatedApp)
    {
      return true;
    }

    string[] showArguments =
    [
      "ad",
      "app",
      "show",
      "--id",
      context.AppId,
      "--query",
      "web.redirectUris",
      "-o",
      "json"
    ];
    CommandOutput showOutput = await context.Cli.CaptureAzAsync(showArguments).ConfigureAwait(false);
    IReadOnlyList<string> urisToWrite = context.DesiredRedirectUris;
    if (!context.Cli.IsDryRun)
    {
      if (!showOutput.Success)
      {
        context.Cli.WriteFailure(showOutput, "Failed to read the app registration redirect URIs.");
        Environment.ExitCode = 1;
        return false;
      }

      if (!EntraAzJson.TryReadStringArray(showOutput.Stdout, out IReadOnlyList<string> existing))
      {
        context.Terminal.WriteErrorLine("Could not parse web.redirectUris from `az ad app show`.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      IReadOnlyList<string> union = EntraRedirectUris.UnionRedirectUris(existing, context.DesiredRedirectUris);
      if (EntraRedirectUris.RedirectUrisEqual(existing, union))
      {
        return true;
      }

      urisToWrite = union;
      context.MergedRedirectUris = true;
      context.DesiredRedirectUris = union;
    }
    else
    {
      context.Terminal.WriteLine("dry-run: merge redirect URIs with `az ad app update --web-redirect-uris` when the union grows.");
    }

    List<string> updateArguments =
    [
      "ad",
      "app",
      "update",
      "--id",
      context.AppId,
      "--web-redirect-uris",
      .. urisToWrite
    ];
    CommandOutput updateOutput = await context.Cli.CaptureAzAsync(updateArguments).ConfigureAwait(false);
    if (!updateOutput.Success)
    {
      context.Cli.WriteFailure(updateOutput, "Failed to update redirect URIs (union, not overwrite).");
      Environment.ExitCode = 1;
      return false;
    }

    if (!context.Cli.IsDryRun)
    {
      context.Terminal.WriteLine("Merged redirect URIs onto the existing app registration.");
    }

    return true;
  }

  internal static async Task<bool> EnsureServicePrincipalAsync(EntraSetupContext context)
  {
    string[] showArguments = ["ad", "sp", "show", "--id", context.AppId, "--query", "id", "-o", "tsv"];
    CommandOutput showOutput = await context.Cli.CaptureAzAsync(showArguments).ConfigureAwait(false);
    string[] createArguments = ["ad", "sp", "create", "--id", context.AppId];
    if (context.Cli.IsDryRun)
    {
      context.Terminal.WriteLine("dry-run: create the service principal when `az ad sp show` does not find one.");
      await context.Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
      return true;
    }

    if (showOutput.Success && showOutput.Stdout.Trim().Length > 0)
    {
      return true;
    }

    CommandOutput createOutput = await context.Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
    if (!createOutput.Success)
    {
      context.Cli.WriteFailure(createOutput, "Failed to create the service principal.");
      Environment.ExitCode = 1;
      return false;
    }

    context.CreatedServicePrincipal = true;
    context.Terminal.WriteLine("Created service principal.");
    return true;
  }
}
