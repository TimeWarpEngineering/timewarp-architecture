#region Purpose
// Mutable state shared by the `dev entra setup` steps.
#endregion

#region Design
// Replaces the old Handler fields so each step is a static method over one context. The runner
// creates it once per run; steps read Options and write what later steps need (tenant, app id,
// minted secret, written keys).
#endregion

namespace DevCli.Services;

internal sealed class EntraSetupContext
{
  internal EntraSetupContext(ITerminal terminal, EntraSetupOptions options, EntraCli cli, string repoRoot)
  {
    Terminal = terminal;
    Options = options;
    Cli = cli;
    RepoRoot = repoRoot;
    DesiredRedirectUris = EntraRedirectUris.BuildDesiredRedirectUris(options.PublicOrigin, options.RedirectUri);
  }

  internal ITerminal Terminal { get; }
  internal EntraSetupOptions Options { get; }
  internal EntraCli Cli { get; }
  internal string RepoRoot { get; }
  internal string DisplayName { get; set; } = EntraSetup.DefaultDisplayName;
  internal IReadOnlyList<string> DesiredRedirectUris { get; set; }
  internal string TenantId { get; set; } = "";
  internal string? TenantDisplayName { get; set; }
  internal string? TenantDomain { get; set; }
  internal EntraTenant SelectedTenant { get; set; } = null!;
  internal string SignedInUser { get; set; } = "";
  internal string SignedInTenantId { get; set; } = "";
  internal string AppId { get; set; } = "";
  internal string? MintedSecret { get; set; }
  internal bool MintedBecauseAppChanged { get; set; }
  internal bool CreatedApp { get; set; }
  internal bool MergedRedirectUris { get; set; }
  internal bool CreatedServicePrincipal { get; set; }
  internal List<string> WrittenSecretKeys { get; } = [];
  internal Dictionary<string, string> ListedSecrets { get; set; } = [];
  internal bool ListedSecretsCaptured { get; set; }
}
