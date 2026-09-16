#region Purpose
// Pure helpers for Entra tenant formatting, Graph/az JSON parse, and tenant/app selection.
#endregion

#region Design
// Compile-included by tests/tools/dev-cli-tests — no Amuru/Terminal. Tenant lines prefer
// display name + default domain over bare GUIDs; domain-only resolved tenants still print
// the domain. SelectTenant matches id/domain/name and refuses ambiguous omitted choice
// (non-interactive CLI). AppLookupNames suffixes the default
// display name with the tenant domain and keeps the bare legacy name for reuse. Graph and
// access-token JSON use JsonDocument (AOT-safe). Secret key strings live on EntraSetup.
#endregion

namespace DevCli.Services;

using System.Text.Json;

internal sealed record EntraTenant(
  string TenantId,
  string? DisplayName,
  string? DefaultDomain,
  bool NameResolved);

internal enum TenantSelectionStatus
{
  Selected,
  Ambiguous,
  NoneVisible,
  NotFound
}

internal sealed record TenantSelection(
  TenantSelectionStatus Status,
  EntraTenant? Selected,
  IReadOnlyList<EntraTenant> Candidates);

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

  internal static bool TryReadOrganization(
    string json,
    out string? id,
    out string? displayName,
    out string? defaultDomain)
  {
    id = null;
    displayName = null;
    defaultDomain = null;
    if (string.IsNullOrWhiteSpace(json))
    {
      return false;
    }

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(json);
    }
    catch (JsonException)
    {
      return false;
    }

    using (document)
    {
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object
        || !root.TryGetProperty("value", out JsonElement value)
        || value.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      if (value.GetArrayLength() == 0)
      {
        return true;
      }

      JsonElement org = value[0];
      if (org.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      if (TryReadStringProperty(org, "id", out string orgId) && orgId.Length > 0)
      {
        id = orgId;
      }

      if (TryReadStringProperty(org, "displayName", out string name) && name.Length > 0)
      {
        displayName = name;
      }

      defaultDomain = ReadDefaultDomain(org);
      return true;
    }
  }

  internal static bool TryReadTenantIdsFromAccountList(string json, out IReadOnlyList<string> ids)
  {
    return TryReadTenantIds(json, preferTenantIdProperty: true, out ids);
  }

  internal static bool TryReadTenantIdsFromTenantList(string json, out IReadOnlyList<string> ids)
  {
    return TryReadTenantIds(json, preferTenantIdProperty: false, out ids);
  }

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

  internal static string DefaultAppDisplayName(string? tenantDomain)
  {
    if (string.IsNullOrWhiteSpace(tenantDomain))
    {
      return EntraSetup.DefaultDisplayName;
    }

    return $"{EntraSetup.DefaultDisplayName} ({tenantDomain.Trim()})";
  }

  internal static IReadOnlyList<string> AppLookupNames(string? explicitName, string? tenantDomain)
  {
    if (!string.IsNullOrWhiteSpace(explicitName))
    {
      return [explicitName.Trim()];
    }

    string preferred = DefaultAppDisplayName(tenantDomain);
    if (string.Equals(preferred, EntraSetup.DefaultDisplayName, StringComparison.Ordinal))
    {
      return [EntraSetup.DefaultDisplayName];
    }

    return [preferred, EntraSetup.DefaultDisplayName];
  }

  internal static void ResolveExistingApp(
    IReadOnlyList<string> preferredIds,
    IReadOnlyList<string> legacyIds,
    out string? appId,
    out bool ambiguous)
  {
    if (preferredIds.Count == 1)
    {
      appId = preferredIds[0];
      ambiguous = false;
      return;
    }

    if (preferredIds.Count > 1)
    {
      appId = null;
      ambiguous = true;
      return;
    }

    if (legacyIds.Count == 1)
    {
      appId = legacyIds[0];
      ambiguous = false;
      return;
    }

    if (legacyIds.Count > 1)
    {
      appId = null;
      ambiguous = true;
      return;
    }

    appId = null;
    ambiguous = false;
  }

  internal static bool PublicOriginMissingFromRedirectUris(
    string? publicOrigin,
    IReadOnlyList<string> redirectUris)
  {
    if (string.IsNullOrWhiteSpace(publicOrigin))
    {
      return false;
    }

    string origin = publicOrigin.Trim().TrimEnd('/');
    foreach (string uri in redirectUris)
    {
      if (uri.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }
    }

    return true;
  }

  internal static string SignInAudienceSummary(string? tenantDomain)
  {
    if (!string.IsNullOrWhiteSpace(tenantDomain))
    {
      return $"Sign in with an @{tenantDomain.Trim()} account (single-tenant app). Other tenants' accounts must be invited as guests first.";
    }

    return "Sign in with an account from this tenant (single-tenant app). Other tenants' accounts must be invited as guests first.";
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

  internal static bool TryReadAccessToken(string json, out string? accessToken)
  {
    accessToken = null;
    if (string.IsNullOrWhiteSpace(json))
    {
      return false;
    }

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(json);
    }
    catch (JsonException)
    {
      return false;
    }

    using (document)
    {
      if (document.RootElement.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      if (!TryReadStringProperty(document.RootElement, "accessToken", out string token)
        || token.Length == 0)
      {
        return false;
      }

      accessToken = token;
      return true;
    }
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

  private static string? ReadDefaultDomain(JsonElement org)
  {
    if (!org.TryGetProperty("verifiedDomains", out JsonElement domains)
      || domains.ValueKind != JsonValueKind.Array)
    {
      return null;
    }

    string? firstName = null;
    foreach (JsonElement domain in domains.EnumerateArray())
    {
      if (domain.ValueKind != JsonValueKind.Object)
      {
        continue;
      }

      if (!TryReadStringProperty(domain, "name", out string name) || name.Length == 0)
      {
        continue;
      }

      firstName ??= name;
      if (domain.TryGetProperty("isDefault", out JsonElement isDefault)
        && isDefault.ValueKind == JsonValueKind.True)
      {
        return name;
      }
    }

    return firstName;
  }

  private static bool TryReadTenantIds(
    string json,
    bool preferTenantIdProperty,
    out IReadOnlyList<string> ids)
  {
    List<string> list = [];
    ids = list;
    if (string.IsNullOrWhiteSpace(json))
    {
      return false;
    }

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(json);
    }
    catch (JsonException)
    {
      return false;
    }

    using (document)
    {
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      foreach (JsonElement element in root.EnumerateArray())
      {
        if (element.ValueKind == JsonValueKind.String)
        {
          string? value = element.GetString();
          if (!string.IsNullOrWhiteSpace(value))
          {
            list.Add(value.Trim());
          }

          continue;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
          continue;
        }

        if (preferTenantIdProperty)
        {
          if (TryReadStringProperty(element, "tenantId", out string tenantId)
            && tenantId.Length > 0)
          {
            list.Add(tenantId.Trim());
          }

          continue;
        }

        if (TryReadStringProperty(element, "tenantId", out string fromTenantId)
          && fromTenantId.Length > 0)
        {
          list.Add(fromTenantId.Trim());
        }
        else if (TryReadStringProperty(element, "id", out string fromId) && fromId.Length > 0)
        {
          list.Add(fromId.Trim());
        }
      }
    }

    return true;
  }

  private static bool TryReadStringProperty(JsonElement element, string name, out string value)
  {
    value = "";
    if (!element.TryGetProperty(name, out JsonElement property))
    {
      return false;
    }

    if (property.ValueKind != JsonValueKind.String)
    {
      return false;
    }

    value = property.GetString() ?? "";
    return true;
  }
}
