#region Purpose
// `dev entra status`: show Entra user secrets (secret masked) and the app registration redirect URIs.
#endregion

#region Design
// Reads Web.Server user secrets first so a missing az login still shows local config. App
// lookup prefers Authentication:Entra:ClientId; falls back to domain-suffixed then bare
// default display names. --tenant enumerates visible tenants for an explicit match (refuse
// on not-found/ambiguous); omitting --tenant reports the tenant from user secrets and does
// not refuse when multiple Azure tenants are visible. Handler stores Command/Ct as fields so
// private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("status", Description = "Show Entra user secrets (masked) and the app registration redirect URIs")]
internal sealed class EntraStatusCommand : EntraGroup, ICommand<Unit>
{
  [Option("tenant", Description = "Tenant id, default domain, or display name (match against visible Azure tenants)")]
  public string? Tenant { get; set; }

  internal sealed class Handler : ICommandHandler<EntraStatusCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private EntraStatusCommand Command = null!;
    private CancellationToken Ct;
    private EntraCli Cli = null!;
    private Dictionary<string, string> Secrets = new(StringComparer.OrdinalIgnoreCase);

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(EntraStatusCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Value;
      }

      Cli = new EntraCli(Terminal, repoRoot, dryRun: false, Ct);

      if (!await ReadSecretsAsync())
      {
        return Value;
      }

      PrintSecrets();
      if (!await PrintTenantLineAsync())
      {
        return Value;
      }

      await PrintAppRegistrationAsync().ConfigureAwait(false);
      return Value;
    }

    private async Task<bool> ReadSecretsAsync()
    {
      CommandOutput output = await Cli.ListUserSecretsAsync().ConfigureAwait(false);
      if (!output.Success)
      {
        Cli.WriteFailure(output, "Failed to list Web.Server user secrets.");
        Environment.ExitCode = 1;
        return false;
      }

      Secrets = EntraSetup.ParseUserSecretsList(output.Stdout);
      return true;
    }

    private void PrintSecrets()
    {
      string[] keys =
      [
        EntraSetup.EnabledKey,
        EntraSetup.TenantIdKey,
        EntraSetup.TenantDisplayNameKey,
        EntraSetup.TenantDomainKey,
        EntraSetup.ClientIdKey,
        EntraSetup.ClientSecretKey,
        EntraSetup.TrustedTenants0Key,
        EntraSetup.AllowBootstrapKey,
        EntraSetup.PublicOriginKey
      ];

      Terminal.WriteLine("User secrets (Authentication:Entra)");
      Terminal.WriteTable
      (
        table =>
        {
          table.AddColumns("Key", "Value");
          foreach (string key in keys)
          {
            string display = SecretDisplay(key);
            table.AddRow(key, display);
          }
        }
      );
    }

    private string SecretDisplay(string key)
    {
      if (!Secrets.TryGetValue(key, out string? value) || string.IsNullOrEmpty(value))
      {
        return "(not set)";
      }

      return key == EntraSetup.ClientSecretKey ? EntraSetup.MaskSecret(value) : value;
    }

    private async Task<bool> PrintTenantLineAsync()
    {
      Terminal.WriteLine("");
      if (!string.IsNullOrWhiteSpace(Command.Tenant))
      {
        if (!EntraCli.AzIsOnPath())
        {
          Terminal.WriteErrorLine("az is not on PATH; cannot resolve --tenant.".Red());
          Environment.ExitCode = 1;
          return false;
        }

        (bool accountOk, string signedInTenantId, _) =
          await EntraTenantDiscovery.TryReadSignedInAccountAsync(Cli, Terminal).ConfigureAwait(false);
        if (!accountOk)
        {
          Environment.ExitCode = 1;
          return false;
        }

        EntraTenantDiscovery discovery = new(Terminal, Cli, signedInTenantId, verboseWarnings: false);
        IReadOnlyList<EntraTenant> tenants = await discovery.EnumerateAsync().ConfigureAwait(false);
        TenantSelection selection = EntraTenants.SelectTenant(tenants, Command.Tenant);
        if (selection.Status is TenantSelectionStatus.NotFound or TenantSelectionStatus.Ambiguous
          or TenantSelectionStatus.NoneVisible)
        {
          if (selection.Status == TenantSelectionStatus.NoneVisible)
          {
            Terminal.WriteErrorLine($"No tenants visible. {EntraCli.AzLoginHint}".Red());
          }
          else if (selection.Status == TenantSelectionStatus.NotFound)
          {
            Terminal.WriteErrorLine(
              $"--tenant '{Command.Tenant.Trim()}' matched no visible tenant.".Red());
          }
          else
          {
            Terminal.WriteErrorLine(
              $"--tenant '{Command.Tenant.Trim()}' matched more than one tenant.".Red());
          }

          EntraTenantDiscovery.PrintCandidateTable(Terminal, selection.Candidates);
          Environment.ExitCode = 1;
          return false;
        }

        Terminal.WriteLine($"Tenant: {EntraTenants.FormatTenantLine(selection.Selected!)}");
        return true;
      }

      if (!Secrets.TryGetValue(EntraSetup.TenantIdKey, out string? tenantId)
        || string.IsNullOrWhiteSpace(tenantId))
      {
        return true;
      }

      Secrets.TryGetValue(EntraSetup.TenantDisplayNameKey, out string? displayName);
      Secrets.TryGetValue(EntraSetup.TenantDomainKey, out string? domain);
      bool resolved = !string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(domain);
      EntraTenant fromSecrets = new(
        tenantId.Trim(),
        string.IsNullOrWhiteSpace(displayName) ? null : displayName,
        string.IsNullOrWhiteSpace(domain) ? null : domain,
        resolved);
      Terminal.WriteLine($"Tenant: {EntraTenants.FormatTenantLine(fromSecrets)}");
      return true;
    }

    private async Task PrintAppRegistrationAsync()
    {
      Terminal.WriteLine("");
      if (!EntraCli.AzIsOnPath())
      {
        Terminal.WriteLine("App registration: skipped (az is not on PATH).");
        return;
      }

      string? clientId = Secrets.TryGetValue(EntraSetup.ClientIdKey, out string? id)
        && !string.IsNullOrWhiteSpace(id)
        ? id
        : null;

      string? appId = clientId;
      if (appId is null)
      {
        appId = await FindAppIdByDisplayNameAsync().ConfigureAwait(false);
      }

      if (appId is null)
      {
        Terminal.WriteLine("App registration: not found (no ClientId in user secrets and no matching TimeWarp Architecture Dev app).");
        return;
      }

      string[] arguments =
      [
        "ad",
        "app",
        "show",
        "--id",
        appId,
        "--query",
        "{appId:appId,displayName:displayName,redirectUris:web.redirectUris}",
        "-o",
        "json"
      ];
      CommandOutput output = await Cli.CaptureAzAsync(arguments).ConfigureAwait(false);
      if (!output.Success)
      {
        Terminal.WriteLine($"App registration: {appId} (az ad app show failed — {EntraCli.AzLoginHint})");
        if (!string.IsNullOrWhiteSpace(output.Stderr))
        {
          Terminal.WriteErrorLine(output.Stderr);
        }

        return;
      }

      if (!EntraSetup.TryReadAppShow(output.Stdout, out string shownAppId, out string displayName, out IReadOnlyList<string> redirectUris))
      {
        Terminal.WriteLine($"App registration: {appId} (could not parse az output)");
        return;
      }

      Terminal.WriteLine($"App registration: {shownAppId} ({displayName})");
      if (redirectUris.Count == 0)
      {
        Terminal.WriteLine("Redirect URIs: (none)");
        return;
      }

      Terminal.WriteLine("Redirect URIs:");
      foreach (string uri in redirectUris)
      {
        Terminal.WriteLine($"  {uri}");
      }
    }

    private async Task<string?> FindAppIdByDisplayNameAsync()
    {
      Secrets.TryGetValue(EntraSetup.TenantDomainKey, out string? domain);
      IReadOnlyList<string> lookupNames = EntraTenants.AppLookupNames(explicitName: null, domain);
      string[] arguments =
      [
        "ad",
        "app",
        "list",
        "--display-name",
        EntraSetup.DefaultDisplayName,
        "--query",
        "[].{appId:appId,displayName:displayName}",
        "-o",
        "json"
      ];
      CommandOutput output = await Cli.CaptureAzAsync(arguments).ConfigureAwait(false);
      if (!output.Success)
      {
        Terminal.WriteLine($"App registration: Azure lookup failed. {EntraCli.AzLoginHint}");
        return null;
      }

      string preferredName = lookupNames[0];
      if (!EntraSetup.TryFindExactAppIds(output.Stdout, preferredName, out IReadOnlyList<string> preferredIds))
      {
        return null;
      }

      IReadOnlyList<string> legacyIds = [];
      if (lookupNames.Count > 1)
      {
        if (!EntraSetup.TryFindExactAppIds(output.Stdout, lookupNames[1], out legacyIds))
        {
          return null;
        }
      }

      EntraTenants.ResolveExistingApp(preferredIds, legacyIds, out string? appId, out bool ambiguous);
      if (ambiguous)
      {
        Terminal.WriteLine($"App registration: multiple matches for '{preferredName}'.");
        return null;
      }

      return appId;
    }
  }
}
