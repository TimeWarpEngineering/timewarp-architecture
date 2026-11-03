#region Purpose
// `dev entra setup` step: pick and validate the target tenant.
#endregion

#region Design
// Non-interactive. Enumerates visible tenants (account list + tenant list + signed-in), resolves
// Graph names read-only under --dry-run, and requires --tenant when more than one is visible.
// A provided --tenant that matches more than one candidate (e.g. two Default Directory orgs)
// prints "matched more than one tenant", not the omitted-flag required message.
#endregion

namespace DevCli.Services;

internal static class EntraSetupTenantStep
{
  internal static async Task<bool> SelectAndValidateTenantAsync(EntraSetupContext context)
  {
    (bool accountOk, string signedInTenantId, string signedInUser) =
      await EntraTenantDiscovery.TryReadSignedInAccountAsync(context.Cli, context.Terminal).ConfigureAwait(false);
    if (!accountOk)
    {
      Environment.ExitCode = 1;
      return false;
    }

    context.SignedInTenantId = signedInTenantId;
    context.SignedInUser = signedInUser;

    EntraTenantDiscovery discovery = new(context.Terminal, context.Cli, context.SignedInTenantId, verboseWarnings: true);
    IReadOnlyList<EntraTenant> tenants = await discovery.EnumerateAsync().ConfigureAwait(false);
    TenantSelection selection = EntraTenants.SelectTenant(tenants, context.Options.Tenant);
    switch (selection.Status)
    {
      case TenantSelectionStatus.NoneVisible:
        context.Terminal.WriteErrorLine($"No tenants visible. {EntraCli.AzLoginHint}".Red());
        Environment.ExitCode = 1;
        return false;

      case TenantSelectionStatus.Ambiguous:
        if (string.IsNullOrWhiteSpace(context.Options.Tenant))
        {
          context.Terminal.WriteErrorLine(
            "--tenant is required when more than one tenant is visible (dev CLI is non-interactive).".Red());
        }
        else
        {
          context.Terminal.WriteErrorLine(
            $"--tenant '{context.Options.Tenant.Trim()}' matched more than one tenant.".Red());
        }

        EntraTenantDiscovery.PrintCandidateTable(context.Terminal, selection.Candidates);
        context.Terminal.WriteLine("Hint: `dev entra setup --tenant <id|domain|name>`");
        Environment.ExitCode = 1;
        return false;

      case TenantSelectionStatus.NotFound:
        context.Terminal.WriteErrorLine(
          $"--tenant '{context.Options.Tenant!.Trim()}' matched no visible tenant.".Red());
        EntraTenantDiscovery.PrintCandidateTable(context.Terminal, selection.Candidates);
        context.Terminal.WriteLine("Hint: `dev entra setup --tenant <id|domain|name>`");
        Environment.ExitCode = 1;
        return false;

      case TenantSelectionStatus.Selected:
        context.SelectedTenant = selection.Selected!;
        break;

      default:
        context.Terminal.WriteErrorLine("Unexpected tenant selection status.".Red());
        Environment.ExitCode = 1;
        return false;
    }

    if (!EntraTenants.TenantIdsEqual(context.SelectedTenant.TenantId, context.SignedInTenantId))
    {
      EntraTenant signedIn = tenants.FirstOrDefault(tenant =>
          EntraTenants.TenantIdsEqual(tenant.TenantId, context.SignedInTenantId))
        ?? new EntraTenant(context.SignedInTenantId, null, null, NameResolved: false);
      context.Terminal.WriteErrorLine(
        $"az is signed into {EntraTenants.FormatTenantLine(signedIn)} but --tenant selected {EntraTenants.FormatTenantLine(context.SelectedTenant)}.".Red());
      context.Terminal.WriteLine(EntraTenants.AzLoginTenantHint(context.SelectedTenant.TenantId));
      Environment.ExitCode = 1;
      return false;
    }

    context.TenantId = context.SelectedTenant.TenantId;
    context.TenantDisplayName = context.SelectedTenant.DisplayName;
    context.TenantDomain = context.SelectedTenant.DefaultDomain;

    context.Terminal.WriteLine($"Tenant: {EntraTenants.FormatTenantLine(context.SelectedTenant)}");
    context.Terminal.WriteLine($"Signed in as: {(string.IsNullOrWhiteSpace(context.SignedInUser) ? "(unknown)" : context.SignedInUser)}");
    return true;
  }
}
