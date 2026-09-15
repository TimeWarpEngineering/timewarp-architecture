#region Purpose
// `dev entra status`: show Entra user secrets (secret masked) and the app registration redirect URIs.
#endregion

#region Design
// Reads Web.Server user secrets first so a missing az login still shows local config. App
// lookup prefers Authentication:Entra:ClientId; falls back to the default display name.
// Handler stores Command/Ct as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("status", Description = "Show Entra user secrets (masked) and the app registration redirect URIs")]
internal sealed class EntraStatusCommand : EntraGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<EntraStatusCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private EntraCli Cli = null!;
    private Dictionary<string, string> Secrets = new(StringComparer.OrdinalIgnoreCase);

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(EntraStatusCommand command, CancellationToken ct)
    {
      if (!EntraCli.TryFindRepoRoot(Terminal, out string repoRoot))
      {
        Environment.ExitCode = 1;
        return Value;
      }

      Cli = new EntraCli(Terminal, repoRoot, dryRun: false, ct);

      if (!await ReadSecretsAsync())
      {
        return Value;
      }

      PrintSecrets();
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
        Terminal.WriteLine("App registration: not found (no ClientId in user secrets and no app named TimeWarp Architecture Dev).");
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

      if (!EntraSetup.TryFindExactAppIds(output.Stdout, EntraSetup.DefaultDisplayName, out IReadOnlyList<string> appIds)
        || appIds.Count == 0)
      {
        return null;
      }

      return appIds[0];
    }
  }
}
