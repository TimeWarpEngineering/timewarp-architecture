#region Purpose
// `dev entra setup`: find-or-create the local Entra app registration and write Web.Server user secrets.
#endregion

#region Design
// Non-interactive. Idempotent by display name: reuse the app, union redirect URIs, ensure the
// service principal, mint a client secret only on first write or --new-secret. A failed
// user-secrets list aborts (does not fail-open mint). The password is captured in memory and
// handed to `dotnet user-secrets set`; it is never printed or placed on a logged command line.
// --dry-run prints every az / user-secrets invocation and executes none.
// Handler stores Command/Ct as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("setup", Description = "Find-or-create the Entra app registration and write Web.Server user secrets")]
[NuruRouteExample("entra setup", Description = "Create or reuse TimeWarp Architecture Dev and write user secrets")]
[NuruRouteExample("entra setup --dry-run", Description = "Print az and user-secrets invocations without running them")]
[NuruRouteExample("entra setup --public-origin https://arch.timewarp.work --new-secret")]
internal sealed class EntraSetupCommand : EntraGroup, ICommand<Unit>
{
  [Option("name", Description = "App registration display name (default: TimeWarp Architecture Dev)")]
  public string? Name { get; set; }

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
    private string SignedInUser = "";
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

      DisplayName = string.IsNullOrWhiteSpace(Command.Name)
        ? EntraSetup.DefaultDisplayName
        : Command.Name.Trim();
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

      if (!await ReadAccountAsync())
      {
        return Value;
      }

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

    private async Task<bool> ReadAccountAsync()
    {
      string[] arguments =
      [
        "account",
        "show",
        "--query",
        "{tenantId:tenantId, user:user.name}",
        "-o",
        "json"
      ];
      CommandOutput output = await Cli.CaptureAzAsync(arguments).ConfigureAwait(false);
      if (Cli.IsDryRun)
      {
        TenantId = "<tenantId>";
        SignedInUser = "<user>";
        return true;
      }

      if (!output.Success)
      {
        Cli.WriteFailure(output, EntraCli.AzLoginHint);
        Environment.ExitCode = 1;
        return false;
      }

      if (!EntraSetup.TryReadAccount(output.Stdout, out TenantId, out SignedInUser))
      {
        Terminal.WriteErrorLine("Could not parse tenant id from `az account show`.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine($"Tenant: {TenantId}");
      Terminal.WriteLine($"Signed in as: {(string.IsNullOrWhiteSpace(SignedInUser) ? "(unknown)" : SignedInUser)}");
      return true;
    }

    private async Task<bool> FindOrCreateAppAsync()
    {
      string[] listArguments =
      [
        "ad",
        "app",
        "list",
        "--display-name",
        DisplayName,
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

      if (!EntraSetup.TryFindExactAppIds(listOutput.Stdout, DisplayName, out IReadOnlyList<string> appIds))
      {
        Terminal.WriteErrorLine("Could not parse `az ad app list` JSON.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (appIds.Count > 1)
      {
        Terminal.WriteErrorLine(
          $"Multiple app registrations named '{DisplayName}'. Rename extras or pass --name.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (appIds.Count == 1)
      {
        AppId = appIds[0];
        Terminal.WriteLine($"Reusing app registration {AppId} ({DisplayName}).");
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
          .AddRow("Tenant", TenantId)
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
      Terminal.WriteLine("Next: `dev run`, browse the app, click \"Continue with Microsoft 365\".".Green());
    }
  }
}
