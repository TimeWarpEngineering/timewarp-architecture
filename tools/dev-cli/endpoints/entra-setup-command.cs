#region Purpose
// `dev entra setup`: find-or-create the local Entra app registration and write Web.Server user secrets.
#endregion

#region Design
// Non-interactive. Enumerates visible tenants (account list + tenant list + signed-in), resolves
// Graph names read-only under --dry-run, and requires --tenant when more than one is visible.
// A provided --tenant that matches more than one candidate (e.g. two Default Directory orgs)
// prints "matched more than one tenant", not the omitted-flag required message.
// Mutations (app create/update, sp, credential reset, user-secrets set) still skip on dry-run.
// Idempotent by display name: prefer domain-suffixed default, reuse bare legacy name if present.
// Mint a client secret only on first write or --new-secret. A failed user-secrets list aborts
// (does not fail-open mint). The password and Graph tokens are never printed. Handler stores
// Command/Ct as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("setup", Description = "Find-or-create the Entra app registration and write Web.Server user secrets")]
[NuruRouteExample("entra setup", Description = "Create or reuse TimeWarp Architecture Dev and write user secrets")]
[NuruRouteExample("entra setup --dry-run", Description = "Print az and user-secrets invocations without running them")]
[NuruRouteExample("entra setup --tenant crunchitfs.com", Description = "Select the tenant by default domain when more than one is visible")]
[NuruRouteExample("entra setup --public-origin https://arch.timewarp.work --new-secret")]
internal sealed class EntraSetupCommand : EntraGroup, ICommand<Unit>
{
  [Option("name", Description = "App registration display name (default: TimeWarp Architecture Dev, suffixed with tenant domain when known)")]
  public string? Name { get; set; }

  [Option("tenant", Description = "Tenant id, default domain, or display name (required when more than one tenant is visible)")]
  public string? Tenant { get; set; }

  [Option("public-origin", Description = "Public HTTPS origin; adds {origin}/signin-oidc to redirect URIs and user secrets")]
  public string? PublicOrigin { get; set; }

  [Option("redirect-uri", Description = "Additional redirect URI (repeatable)")]
  public string[] RedirectUri { get; set; } = [];

  [Option("new-secret", Description = "Mint a new client secret even if one is already in user secrets")]
  public bool NewSecret { get; set; }

  [Option("dry-run", Description = "Print az and user-secrets invocations without running them")]
  public bool DryRun { get; set; }

  internal sealed class Handler : ICommandHandler<EntraSetupCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private EntraSetupCommand Command = null!;
    private CancellationToken Ct;
    private EntraCli Cli = null!;
    private string DisplayName = EntraSetup.DefaultDisplayName;
    private IReadOnlyList<string> DesiredRedirectUris = [];
    private string TenantId = "";
    private string? TenantDisplayName;
    private string? TenantDomain;
    private EntraTenant SelectedTenant = null!;
    private string SignedInUser = "";
    private string SignedInTenantId = "";
    private string AppId = "";
    private string? MintedSecret;
    private bool CreatedApp;
    private bool MergedRedirectUris;
    private bool CreatedServicePrincipal;
    private readonly List<string> WrittenSecretKeys = [];

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(EntraSetupCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;
      DesiredRedirectUris = EntraSetup.BuildDesiredRedirectUris(Command.PublicOrigin, Command.RedirectUri);

      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Value;
      }

      Cli = new EntraCli(Terminal, repoRoot, Command.DryRun, Ct);

      if (!PreflightAz())
      {
        return Value;
      }

      if (!await SelectAndValidateTenantAsync())
      {
        return Value;
      }

      DisplayName = string.IsNullOrWhiteSpace(Command.Name)
        ? EntraTenants.DefaultAppDisplayName(TenantDomain)
        : Command.Name.Trim();

      if (!await FindOrCreateAppAsync())
      {
        return Value;
      }

      if (!await MergeRedirectUrisAsync())
      {
        return Value;
      }

      if (!await EnsureServicePrincipalAsync())
      {
        return Value;
      }

      if (!await MaybeMintSecretAsync())
      {
        return Value;
      }

      if (!await WriteUserSecretsAsync())
      {
        return Value;
      }

      PrintSummary();
      return Value;
    }

    private bool PreflightAz()
    {
      if (EntraCli.AzIsOnPath())
      {
        return true;
      }

      Terminal.WriteErrorLine("az is not on PATH. Install Azure CLI and retry.".Red());
      Environment.ExitCode = 1;
      return false;
    }

    private async Task<bool> SelectAndValidateTenantAsync()
    {
      (bool accountOk, string signedInTenantId, string signedInUser) =
        await EntraTenantDiscovery.TryReadSignedInAccountAsync(Cli, Terminal).ConfigureAwait(false);
      if (!accountOk)
      {
        Environment.ExitCode = 1;
        return false;
      }

      SignedInTenantId = signedInTenantId;
      SignedInUser = signedInUser;

      EntraTenantDiscovery discovery = new(Terminal, Cli, SignedInTenantId, verboseWarnings: true);
      IReadOnlyList<EntraTenant> tenants = await discovery.EnumerateAsync().ConfigureAwait(false);
      TenantSelection selection = EntraTenants.SelectTenant(tenants, Command.Tenant);
      switch (selection.Status)
      {
        case TenantSelectionStatus.NoneVisible:
          Terminal.WriteErrorLine($"No tenants visible. {EntraCli.AzLoginHint}".Red());
          Environment.ExitCode = 1;
          return false;

        case TenantSelectionStatus.Ambiguous:
          if (string.IsNullOrWhiteSpace(Command.Tenant))
          {
            Terminal.WriteErrorLine(
              "--tenant is required when more than one tenant is visible (dev CLI is non-interactive).".Red());
          }
          else
          {
            Terminal.WriteErrorLine(
              $"--tenant '{Command.Tenant.Trim()}' matched more than one tenant.".Red());
          }

          EntraTenantDiscovery.PrintCandidateTable(Terminal, selection.Candidates);
          Terminal.WriteLine("Hint: `dev entra setup --tenant <id|domain|name>`");
          Environment.ExitCode = 1;
          return false;

        case TenantSelectionStatus.NotFound:
          Terminal.WriteErrorLine(
            $"--tenant '{Command.Tenant!.Trim()}' matched no visible tenant.".Red());
          EntraTenantDiscovery.PrintCandidateTable(Terminal, selection.Candidates);
          Terminal.WriteLine("Hint: `dev entra setup --tenant <id|domain|name>`");
          Environment.ExitCode = 1;
          return false;

        case TenantSelectionStatus.Selected:
          SelectedTenant = selection.Selected!;
          break;

        default:
          Terminal.WriteErrorLine("Unexpected tenant selection status.".Red());
          Environment.ExitCode = 1;
          return false;
      }

      if (!EntraTenants.TenantIdsEqual(SelectedTenant.TenantId, SignedInTenantId))
      {
        EntraTenant signedIn = tenants.FirstOrDefault(tenant =>
            EntraTenants.TenantIdsEqual(tenant.TenantId, SignedInTenantId))
          ?? new EntraTenant(SignedInTenantId, null, null, NameResolved: false);
        Terminal.WriteErrorLine(
          $"az is signed into {EntraTenants.FormatTenantLine(signedIn)} but --tenant selected {EntraTenants.FormatTenantLine(SelectedTenant)}.".Red());
        Terminal.WriteLine(EntraTenants.AzLoginTenantHint(SelectedTenant.TenantId));
        Environment.ExitCode = 1;
        return false;
      }

      TenantId = SelectedTenant.TenantId;
      TenantDisplayName = SelectedTenant.DisplayName;
      TenantDomain = SelectedTenant.DefaultDomain;

      Terminal.WriteLine($"Tenant: {EntraTenants.FormatTenantLine(SelectedTenant)}");
      Terminal.WriteLine($"Signed in as: {(string.IsNullOrWhiteSpace(SignedInUser) ? "(unknown)" : SignedInUser)}");
      return true;
    }

    private async Task<bool> FindOrCreateAppAsync()
    {
      IReadOnlyList<string> lookupNames = EntraTenants.AppLookupNames(Command.Name, TenantDomain);
      string listDisplayName = string.IsNullOrWhiteSpace(Command.Name)
        ? EntraSetup.DefaultDisplayName
        : Command.Name.Trim();
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
      CommandOutput listOutput = await Cli.CaptureAzAsync(listArguments).ConfigureAwait(false);
      List<string> createArguments =
      [
        "ad",
        "app",
        "create",
        "--display-name",
        DisplayName,
        "--sign-in-audience",
        "AzureADMyOrg",
        "--web-redirect-uris",
        .. DesiredRedirectUris,
        "--query",
        "appId",
        "-o",
        "tsv"
      ];
      if (Cli.IsDryRun)
      {
        AppId = "<appId>";
        Terminal.WriteLine("dry-run: create the app when the display name is absent; otherwise reuse it.");
        await Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
        return true;
      }

      if (!listOutput.Success)
      {
        Cli.WriteFailure(listOutput, "Failed to list Entra app registrations.");
        Environment.ExitCode = 1;
        return false;
      }

      if (!EntraSetup.TryFindExactAppIds(listOutput.Stdout, preferredName, out IReadOnlyList<string> preferredIds))
      {
        Terminal.WriteErrorLine("Could not parse `az ad app list` JSON.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      IReadOnlyList<string> legacyIds = [];
      if (legacyName is not null)
      {
        if (!EntraSetup.TryFindExactAppIds(listOutput.Stdout, legacyName, out legacyIds))
        {
          Terminal.WriteErrorLine("Could not parse `az ad app list` JSON.".Red());
          Environment.ExitCode = 1;
          return false;
        }
      }

      EntraTenants.ResolveExistingApp(preferredIds, legacyIds, out string? existingAppId, out bool ambiguous);
      if (ambiguous)
      {
        Terminal.WriteErrorLine(
          $"Multiple app registrations named '{preferredName}'. Rename extras or pass --name.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (existingAppId is not null)
      {
        AppId = existingAppId;
        string reusedName = preferredIds.Count == 1
          ? preferredName
          : legacyName ?? preferredName;
        Terminal.WriteLine($"Reusing app registration {AppId} ({reusedName}).");
        DisplayName = reusedName;
        return true;
      }

      CommandOutput createOutput = await Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
      if (!createOutput.Success)
      {
        Cli.WriteFailure(createOutput, "Failed to create the Entra app registration.");
        Environment.ExitCode = 1;
        return false;
      }

      AppId = createOutput.Stdout.Trim();
      if (AppId.Length == 0)
      {
        Terminal.WriteErrorLine("az ad app create succeeded but returned no appId.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      CreatedApp = true;
      Terminal.WriteLine($"Created app registration {AppId} ({DisplayName}).");
      return true;
    }

    private async Task<bool> MergeRedirectUrisAsync()
    {
      if (CreatedApp)
      {
        return true;
      }

      string[] showArguments =
      [
        "ad",
        "app",
        "show",
        "--id",
        AppId,
        "--query",
        "web.redirectUris",
        "-o",
        "json"
      ];
      CommandOutput showOutput = await Cli.CaptureAzAsync(showArguments).ConfigureAwait(false);
      IReadOnlyList<string> urisToWrite = DesiredRedirectUris;
      if (!Cli.IsDryRun)
      {
        if (!showOutput.Success)
        {
          Cli.WriteFailure(showOutput, "Failed to read the app registration redirect URIs.");
          Environment.ExitCode = 1;
          return false;
        }

        if (!EntraSetup.TryReadStringArray(showOutput.Stdout, out IReadOnlyList<string> existing))
        {
          Terminal.WriteErrorLine("Could not parse web.redirectUris from `az ad app show`.".Red());
          Environment.ExitCode = 1;
          return false;
        }

        IReadOnlyList<string> union = EntraSetup.UnionRedirectUris(existing, DesiredRedirectUris);
        if (EntraSetup.RedirectUrisEqual(existing, union))
        {
          return true;
        }

        urisToWrite = union;
        MergedRedirectUris = true;
        DesiredRedirectUris = union;
      }
      else
      {
        Terminal.WriteLine("dry-run: merge redirect URIs with `az ad app update --web-redirect-uris` when the union grows.");
      }

      List<string> updateArguments =
      [
        "ad",
        "app",
        "update",
        "--id",
        AppId,
        "--web-redirect-uris",
        .. urisToWrite
      ];
      CommandOutput updateOutput = await Cli.CaptureAzAsync(updateArguments).ConfigureAwait(false);
      if (!updateOutput.Success)
      {
        Cli.WriteFailure(updateOutput, "Failed to update redirect URIs (union, not overwrite).");
        Environment.ExitCode = 1;
        return false;
      }

      if (!Cli.IsDryRun)
      {
        Terminal.WriteLine("Merged redirect URIs onto the existing app registration.");
      }

      return true;
    }

    private async Task<bool> EnsureServicePrincipalAsync()
    {
      string[] showArguments = ["ad", "sp", "show", "--id", AppId, "--query", "id", "-o", "tsv"];
      CommandOutput showOutput = await Cli.CaptureAzAsync(showArguments).ConfigureAwait(false);
      string[] createArguments = ["ad", "sp", "create", "--id", AppId];
      if (Cli.IsDryRun)
      {
        Terminal.WriteLine("dry-run: create the service principal when `az ad sp show` does not find one.");
        await Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
        return true;
      }

      if (showOutput.Success && showOutput.Stdout.Trim().Length > 0)
      {
        return true;
      }

      CommandOutput createOutput = await Cli.CaptureAzAsync(createArguments).ConfigureAwait(false);
      if (!createOutput.Success)
      {
        Cli.WriteFailure(createOutput, "Failed to create the service principal.");
        Environment.ExitCode = 1;
        return false;
      }

      CreatedServicePrincipal = true;
      Terminal.WriteLine("Created service principal.");
      return true;
    }

    private async Task<bool> MaybeMintSecretAsync()
    {
      bool mint = Command.NewSecret;
      if (!mint && !Cli.IsDryRun)
      {
        CommandOutput listOutput = await Cli.ListUserSecretsAsync().ConfigureAwait(false);
        Dictionary<string, string> secrets = listOutput.Success
          ? EntraSetup.ParseUserSecretsList(listOutput.Stdout)
          : [];
        if (!EntraSetup.TryDecideMintClientSecret(
          Command.NewSecret,
          listOutput.Success,
          EntraSetup.HasClientSecret(secrets),
          out mint))
        {
          Cli.WriteFailure(listOutput, "Failed to list Web.Server user secrets; not minting a client secret.");
          Environment.ExitCode = 1;
          return false;
        }
      }

      if (Cli.IsDryRun)
      {
        Terminal.WriteLine(
          "dry-run: mint a client secret when --new-secret is set or Authentication:Entra:ClientSecret is absent.");
      }

      if (!mint && !Cli.IsDryRun)
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
        AppId,
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
      CommandOutput output = await Cli.CaptureAzSecretAsync(arguments).ConfigureAwait(false);
      if (Cli.IsDryRun)
      {
        MintedSecret = EntraSetup.MaskedSecret;
        return true;
      }

      if (!output.Success)
      {
        Cli.WriteFailure(output, "Failed to mint an app credential. Stdout (the password) was not printed.");
        Environment.ExitCode = 1;
        return false;
      }

      MintedSecret = output.Stdout.Trim();
      if (string.IsNullOrEmpty(MintedSecret))
      {
        Terminal.WriteErrorLine("az ad app credential reset returned an empty password.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine($"Minted client secret `{displayName}` (value not printed).");
      return true;
    }

    private async Task<bool> WriteUserSecretsAsync()
    {
      List<(string Key, string Value, bool Mask)> pairs =
      [
        (EntraSetup.EnabledKey, "true", false),
        (EntraSetup.TenantIdKey, TenantId, false),
        (EntraSetup.ClientIdKey, AppId, false),
        (EntraSetup.TrustedTenants0Key, TenantId, false),
        (EntraSetup.AllowBootstrapKey, "true", false)
      ];

      if (!string.IsNullOrWhiteSpace(TenantDisplayName))
      {
        pairs.Add((EntraSetup.TenantDisplayNameKey, TenantDisplayName, false));
      }

      if (!string.IsNullOrWhiteSpace(TenantDomain))
      {
        pairs.Add((EntraSetup.TenantDomainKey, TenantDomain, false));
      }

      if (MintedSecret is not null)
      {
        pairs.Add((EntraSetup.ClientSecretKey, MintedSecret, true));
      }

      if (!string.IsNullOrWhiteSpace(Command.PublicOrigin))
      {
        pairs.Add((EntraSetup.PublicOriginKey, Command.PublicOrigin.Trim().TrimEnd('/'), false));
      }

      foreach ((string key, string value, bool mask) in pairs)
      {
        CommandOutput output = await Cli.SetUserSecretAsync(key, value, mask).ConfigureAwait(false);
        if (!output.Success)
        {
          Cli.WriteFailure(output, $"Failed to write user secret {key}.");
          Environment.ExitCode = 1;
          return false;
        }

        WrittenSecretKeys.Add(mask ? $"{key}={EntraSetup.MaskedSecret}" : key);
      }

      return true;
    }

    private void PrintSummary()
    {
      if (Cli.IsDryRun)
      {
        Terminal.WriteLine("Dry-run: no Azure or user-secrets changes.");
      }

      string secretStatus = MintedSecret is null
        ? "(unchanged)"
        : EntraSetup.MaskedSecret + " (minted this run)";
      string written = WrittenSecretKeys.Count == 0
        ? "(none)"
        : string.Join(", ", WrittenSecretKeys);

      Terminal.WriteLine("");
      Terminal.WriteTable
      (
        table => table
          .AddColumns("Field", "Value")
          .AddRow("Tenant", EntraTenants.FormatTenantLine(SelectedTenant))
          .AddRow("Signed in as", string.IsNullOrWhiteSpace(SignedInUser) ? "(unknown)" : SignedInUser)
          .AddRow("Display name", DisplayName)
          .AddRow("App id", AppId)
          .AddRow("Created app", CreatedApp ? "yes" : "no")
          .AddRow("Merged redirect URIs", MergedRedirectUris ? "yes" : "no")
          .AddRow("Created service principal", CreatedServicePrincipal ? "yes" : "no")
          .AddRow("Redirect URIs", string.Join(" ", DesiredRedirectUris))
          .AddRow("Secrets written", written)
          .AddRow("Client secret", secretStatus)
      );

      Terminal.WriteLine("");
      Terminal.WriteLine(EntraTenants.SignInAudienceSummary(TenantDomain));
      if (EntraTenants.PublicOriginMissingFromRedirectUris(Command.PublicOrigin, DesiredRedirectUris))
      {
        Terminal.WriteLine(
          $"Warning: --public-origin '{Command.PublicOrigin!.Trim()}' was given but no redirect URI starts with it.".Yellow());
      }

      Terminal.WriteLine("");
      Terminal.WriteLine("Next: `dev run`, browse the app, click \"Continue with Microsoft 365\".".Green());
    }
  }
}
