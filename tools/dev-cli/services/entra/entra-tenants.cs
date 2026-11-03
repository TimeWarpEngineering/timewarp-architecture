#region Purpose
// Tenant line formatting, selection, merge, and id comparison for `dev entra`.
#endregion

#region Design
// Compile-included by tests/tools/dev-cli-tests — no Amuru/Terminal. Tenant lines prefer
// display name + default domain over bare GUIDs; domain-only resolved tenants still print
// the domain. SelectTenant matches id/domain/name and refuses ambiguous omitted choice
// (non-interactive CLI). Graph/az JSON readers live in EntraTenantJson; app lookup names and
// sign-in text live in EntraAppLookup. Secret key strings live on EntraSetup.
#endregion

namespace DevCli.Services;

internal static class EntraTenants
{
  internal static string FormatTenantLine(EntraTenant tenant)
  {
    bool hasName = !string.IsNullOrWhiteSpace(tenant.DisplayName);
    bool hasDomain = !string.IsNullOrWhiteSpace(tenant.DefaultDomain);
    if (!tenant.NameResolved || (!hasName && !hasDomain))
    {
      return $"{tenant.TenantId} (name unavailable)";
    }

    if (hasName && hasDomain)
    {
      return $"{tenant.DisplayName} ({tenant.DefaultDomain}) — {tenant.TenantId}";
    }

    if (hasName)
    {
      return $"{tenant.DisplayName} — {tenant.TenantId}";
    }

    return $"{tenant.DefaultDomain} — {tenant.TenantId}";
  }

  internal static string AzLoginTenantHint(string tenantId) =>
    $"az login --tenant {tenantId} --allow-no-subscriptions";

  internal static IReadOnlyList<string> CollectTenantIds(
    IEnumerable<string> first,
    IEnumerable<string> second)
  {
    List<string> collected = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    foreach (string id in first.Concat(second))
    {
      if (string.IsNullOrWhiteSpace(id))
      {
        continue;
      }

      string trimmed = id.Trim();
      if (seen.Add(trimmed))
      {
        collected.Add(trimmed);
      }
    }

    return collected;
  }

  internal static EntraTenant MergeTenant(EntraTenant existing, EntraTenant incoming)
  {
    string tenantId = existing.TenantId.Length > 0 ? existing.TenantId : incoming.TenantId;
    string? displayName = PreferResolved(existing.DisplayName, incoming.DisplayName, existing.NameResolved, incoming.NameResolved);
    string? defaultDomain = PreferResolved(existing.DefaultDomain, incoming.DefaultDomain, existing.NameResolved, incoming.NameResolved);
    bool nameResolved = existing.NameResolved || incoming.NameResolved;
    if (!nameResolved
      && (!string.IsNullOrWhiteSpace(displayName) || !string.IsNullOrWhiteSpace(defaultDomain)))
    {
      nameResolved = true;
    }

    return new EntraTenant(tenantId, displayName, defaultDomain, nameResolved);
  }

  internal static TenantSelection SelectTenant(IReadOnlyList<EntraTenant> tenants, string? tenantOption)
  {
    string? option = string.IsNullOrWhiteSpace(tenantOption) ? null : tenantOption.Trim();
    if (option is null)
    {
      if (tenants.Count == 0)
      {
        return new TenantSelection(TenantSelectionStatus.NoneVisible, null, tenants);
      }

      if (tenants.Count == 1)
      {
        return new TenantSelection(TenantSelectionStatus.Selected, tenants[0], tenants);
      }

      return new TenantSelection(TenantSelectionStatus.Ambiguous, null, tenants);
    }

    List<EntraTenant> matches = [];
    foreach (EntraTenant tenant in tenants)
    {
      if (MatchesTenant(tenant, option))
      {
        matches.Add(tenant);
      }
    }

    if (matches.Count == 0)
    {
      return new TenantSelection(TenantSelectionStatus.NotFound, null, tenants);
    }

    if (matches.Count == 1)
    {
      return new TenantSelection(TenantSelectionStatus.Selected, matches[0], matches);
    }

    return new TenantSelection(TenantSelectionStatus.Ambiguous, null, matches);
  }

  internal static bool TenantIdsEqual(string left, string right)
  {
    if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (Guid.TryParse(left, out Guid leftGuid) && Guid.TryParse(right, out Guid rightGuid))
    {
      return leftGuid == rightGuid;
    }

    return false;
  }

  private static bool MatchesTenant(EntraTenant tenant, string option)
  {
    if (TenantIdsEqual(tenant.TenantId, option))
    {
      return true;
    }

    if (!string.IsNullOrWhiteSpace(tenant.DefaultDomain)
      && string.Equals(tenant.DefaultDomain, option, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (!string.IsNullOrWhiteSpace(tenant.DisplayName)
      && string.Equals(tenant.DisplayName, option, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return false;
  }

  private static string? PreferResolved(
    string? existing,
    string? incoming,
    bool existingResolved,
    bool incomingResolved)
  {
    if (existingResolved && !string.IsNullOrWhiteSpace(existing))
    {
      return existing;
    }

    if (incomingResolved && !string.IsNullOrWhiteSpace(incoming))
    {
      return incoming;
    }

    if (!string.IsNullOrWhiteSpace(existing))
    {
      return existing;
    }

    return string.IsNullOrWhiteSpace(incoming) ? null : incoming;
  }
}
