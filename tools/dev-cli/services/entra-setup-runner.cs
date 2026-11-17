#region Purpose
// Runs `dev entra setup`: preflight, then the tenant, app, and secrets steps in order, then the summary.
#endregion

#region Design
// Orchestration only; each step lives in EntraSetupTenantStep, EntraSetupAppStep, and
// EntraSetupSecretsStep and returns false after setting Environment.ExitCode on failure.
// Mutations (app create/update, sp, credential reset, user-secrets set) skip on dry-run.
// The password and Graph tokens are never printed. Process calls go through EntraCli (Amuru).
#endregion

namespace DevCli.Services;

internal static class EntraSetupRunner
{
  internal static async Task RunAsync(ITerminal terminal, EntraSetupOptions options, CancellationToken ct)
  {
    if (!EntraCli.TryFindRepoRoot(terminal, out string repoRoot))
    {
      Environment.ExitCode = 1;
      return;
    }

    EntraCli cli = new(terminal, repoRoot, options.DryRun, ct);
    EntraSetupContext context = new(terminal, options, cli, repoRoot);

    if (!PreflightAz(context))
    {
      return;
    }

    if (!await EntraSetupTenantStep.SelectAndValidateTenantAsync(context).ConfigureAwait(false))
    {
      return;
    }

    context.DisplayName = string.IsNullOrWhiteSpace(options.Name)
      ? EntraAppLookup.DefaultAppDisplayName(context.TenantDomain)
      : options.Name.Trim();

    if (!await EntraSetupAppStep.FindOrCreateAppAsync(context).ConfigureAwait(false))
    {
      return;
    }

    if (!await EntraSetupAppStep.MergeRedirectUrisAsync(context).ConfigureAwait(false))
    {
      return;
    }

    if (!await EntraSetupAppStep.EnsureServicePrincipalAsync(context).ConfigureAwait(false))
    {
      return;
    }

    if (!await EntraSetupSecretsStep.MaybeMintSecretAsync(context).ConfigureAwait(false))
    {
      return;
    }

    if (!await EntraSetupSecretsStep.WriteUserSecretsAsync(context).ConfigureAwait(false))
    {
      return;
    }

    PrintSummary(context);
  }

  private static bool PreflightAz(EntraSetupContext context)
  {
    if (EntraCli.AzIsOnPath())
    {
      return true;
    }

    context.Terminal.WriteErrorLine("az is not on PATH. Install Azure CLI and retry.".Red());
    Environment.ExitCode = 1;
    return false;
  }

  private static void PrintSummary(EntraSetupContext context)
  {
    if (context.Cli.IsDryRun)
    {
      context.Terminal.WriteLine("Dry-run: no Azure or user-secrets changes.");
    }

    string secretStatus = EntraClientSecrets.FormatClientSecretSummary(
      context.MintedSecret is not null,
      context.MintedBecauseAppChanged);
    string written = context.WrittenSecretKeys.Count == 0
      ? "(none)"
      : string.Join(", ", context.WrittenSecretKeys);

    context.Terminal.WriteLine("");
    context.Terminal.WriteTable
    (
      table => table
        .AddColumns("Field", "Value")
        .AddRow("Tenant", EntraTenants.FormatTenantLine(context.SelectedTenant))
        .AddRow("Signed in as", string.IsNullOrWhiteSpace(context.SignedInUser) ? "(unknown)" : context.SignedInUser)
        .AddRow("Display name", context.DisplayName)
        .AddRow("App id", context.AppId)
        .AddRow("Created app", context.CreatedApp ? "yes" : "no")
        .AddRow("Merged redirect URIs", context.MergedRedirectUris ? "yes" : "no")
        .AddRow("Created service principal", context.CreatedServicePrincipal ? "yes" : "no")
        .AddRow("Redirect URIs", string.Join(" ", context.DesiredRedirectUris))
        .AddRow("Secrets written", written)
        .AddRow("Client secret", secretStatus)
    );

    context.Terminal.WriteLine("");
    context.Terminal.WriteLine(EntraAppLookup.SignInAudienceSummary(context.TenantDomain));
    context.Terminal.WriteLine(EntraAppLookup.MultiTenantAcceptanceNote());
    if (EntraAppLookup.PublicOriginMissingFromRedirectUris(context.Options.PublicOrigin, context.DesiredRedirectUris))
    {
      context.Terminal.WriteLine(
        $"Warning: --public-origin '{context.Options.PublicOrigin!.Trim()}' was given but no redirect URI starts with it.".Yellow());
    }

    context.Terminal.WriteLine("");
    context.Terminal.WriteLine("Next: `dev run`, browse the app, click \"Continue with Microsoft 365\".".Green());
  }
}
