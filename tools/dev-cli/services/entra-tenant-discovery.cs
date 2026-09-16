#region Purpose
// Shared read-only Azure tenant enumeration for `dev entra setup` and `dev entra status`.
#endregion

#region Design
// Process layer (EntraCli + ITerminal) so tests stay on EntraTenants pure helpers. Graph and
// get-access-token failures never abort enumeration — unresolved tenants keep the GUID and an
// az login --tenant hint. Does not run az login or az config set. Always executes under
// --dry-run (read-only). Bearer headers go through CaptureAzReadOnlyMaskedAsync.
#endregion

namespace DevCli.Services;

internal sealed class EntraTenantDiscovery
{
  private readonly ITerminal Terminal;
  private readonly EntraCli Cli;
  private readonly string SignedInTenantId;
  private readonly bool VerboseWarnings;

  internal EntraTenantDiscovery(
    ITerminal terminal,
    EntraCli cli,
    string signedInTenantId,
    bool verboseWarnings)
  {
    Terminal = terminal;
    Cli = cli;
    SignedInTenantId = signedInTenantId;
    VerboseWarnings = verboseWarnings;
  }

  internal static async Task<(bool Success, string TenantId, string User)> TryReadSignedInAccountAsync(
    EntraCli cli,
    ITerminal terminal)
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
    CommandOutput output = await cli.CaptureAzReadOnlyAsync(arguments).ConfigureAwait(false);
    if (!output.Success)
    {
      cli.WriteFailure(output, EntraCli.AzLoginHint);
      return (false, "", "");
    }

    if (!EntraSetup.TryReadAccount(output.Stdout, out string tenantId, out string user))
    {
      terminal.WriteErrorLine("Could not parse tenant id from `az account show`.".Red());
      return (false, "", "");
    }

    return (true, tenantId, user);
  }

  internal async Task<IReadOnlyList<EntraTenant>> EnumerateAsync()
  {
    List<string> fromAccount = [];
    List<string> fromTenantList = [];
    if (!string.IsNullOrWhiteSpace(SignedInTenantId))
    {
      fromAccount.Add(SignedInTenantId);
    }

    string[] accountListArguments = ["account", "list", "-o", "json"];
    CommandOutput accountListOutput = await Cli.CaptureAzReadOnlyAsync(accountListArguments).ConfigureAwait(false);
    if (accountListOutput.Success)
    {
      if (EntraTenants.TryReadTenantIdsFromAccountList(accountListOutput.Stdout, out IReadOnlyList<string> accountIds))
      {
        fromAccount.AddRange(accountIds);
      }
      else if (VerboseWarnings)
      {
        Terminal.WriteLine("Warning: could not parse `az account list` JSON; using signed-in tenant only.");
      }
    }
    else if (VerboseWarnings)
    {
      Terminal.WriteLine("Warning: `az account list` failed; using signed-in tenant only.");
    }

    string[] tenantListArguments = ["account", "tenant", "list", "-o", "json"];
    CommandOutput tenantListOutput = await Cli.CaptureAzReadOnlyAsync(tenantListArguments).ConfigureAwait(false);
    if (tenantListOutput.Success)
    {
      if (EntraTenants.TryReadTenantIdsFromTenantList(tenantListOutput.Stdout, out IReadOnlyList<string> tenantIds))
      {
        fromTenantList.AddRange(tenantIds);
      }
      else if (VerboseWarnings)
      {
        Terminal.WriteLine("Warning: could not parse `az account tenant list` JSON.");
      }
    }
    else
    {
      Terminal.WriteLine(
        "Note: `az account tenant list` unavailable; subscription-less tenants were not listed.");
    }

    IReadOnlyList<string> uniqueIds = EntraTenants.CollectTenantIds(fromAccount, fromTenantList);
    List<EntraTenant> tenants = [];
    foreach (string tenantId in uniqueIds)
    {
      tenants.Add(await ResolveTenantAsync(tenantId).ConfigureAwait(false));
    }

    return tenants;
  }

  internal static void PrintCandidateTable(ITerminal terminal, IReadOnlyList<EntraTenant> tenants)
  {
    terminal.WriteLine("");
    terminal.WriteTable
    (
      table =>
      {
        table.AddColumns("Tenant", "Domain", "Id", "Notes");
        foreach (EntraTenant tenant in tenants)
        {
          string name = tenant.NameResolved && !string.IsNullOrWhiteSpace(tenant.DisplayName)
            ? tenant.DisplayName
            : "(name unavailable)";
          string domain = string.IsNullOrWhiteSpace(tenant.DefaultDomain)
            ? "(unknown)"
            : tenant.DefaultDomain;
          string notes = tenant.NameResolved
            ? ""
            : $"name unavailable — run: {EntraTenants.AzLoginTenantHint(tenant.TenantId)}";
          table.AddRow(name, domain, tenant.TenantId, notes);
        }
      }
    );

    // WriteTable truncates to the terminal width; print full lines for --tenant copy-paste.
    terminal.WriteLine("Full values for --tenant:");
    foreach (EntraTenant tenant in tenants)
    {
      terminal.WriteLine($"  {EntraTenants.FormatTenantLine(tenant)}");
    }
  }

  private async Task<EntraTenant> ResolveTenantAsync(string tenantId)
  {
    if (EntraTenants.TenantIdsEqual(tenantId, SignedInTenantId))
    {
      string[] arguments =
      [
        "rest",
        "--method",
        "get",
        "--url",
        EntraCli.GraphOrganizationUrl
      ];
      CommandOutput output = await Cli.CaptureAzReadOnlyAsync(arguments).ConfigureAwait(false);
      if (output.Success
        && EntraTenants.TryReadOrganization(output.Stdout, out _, out string? displayName, out string? defaultDomain))
      {
        bool resolved = !string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(defaultDomain);
        return new EntraTenant(tenantId, displayName, defaultDomain, resolved);
      }

      if (VerboseWarnings)
      {
        Terminal.WriteLine(
          $"Warning: Graph organization lookup failed for signed-in tenant {tenantId}; name unavailable.");
      }

      return new EntraTenant(tenantId, null, null, NameResolved: false);
    }

    string[] tokenArguments =
    [
      "account",
      "get-access-token",
      "--tenant",
      tenantId,
      "--resource",
      EntraCli.GraphResource,
      "-o",
      "json"
    ];
    CommandOutput tokenOutput = await Cli.CaptureAzTokenAsync(tokenArguments).ConfigureAwait(false);
    if (!tokenOutput.Success
      || !EntraTenants.TryReadAccessToken(tokenOutput.Stdout, out string? accessToken)
      || string.IsNullOrEmpty(accessToken))
    {
      return new EntraTenant(tenantId, null, null, NameResolved: false);
    }

    string[] graphArguments =
    [
      "rest",
      "--method",
      "get",
      "--url",
      EntraCli.GraphOrganizationUrl,
      "--headers",
      $"Authorization=Bearer {accessToken}"
    ];
    CommandOutput graphOutput = await Cli.CaptureAzReadOnlyMaskedAsync(graphArguments).ConfigureAwait(false);
    if (graphOutput.Success
      && EntraTenants.TryReadOrganization(graphOutput.Stdout, out _, out string? otherName, out string? otherDomain))
    {
      bool resolved = !string.IsNullOrWhiteSpace(otherName) || !string.IsNullOrWhiteSpace(otherDomain);
      return new EntraTenant(tenantId, otherName, otherDomain, resolved);
    }

    return new EntraTenant(tenantId, null, null, NameResolved: false);
  }
}
