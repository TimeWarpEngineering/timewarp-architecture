#region Purpose
// Parse Graph organization, az account/tenant list, and access-token JSON for tenant discovery.
#endregion

#region Design
// Graph and access-token JSON use JsonDocument (AOT-safe). Property reads reuse
// EntraAzJson.TryReadStringProperty.
#endregion

namespace DevCli.Services;

using System.Text.Json;

internal static class EntraTenantJson
{
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

      if (EntraAzJson.TryReadStringProperty(org, "id", out string orgId) && orgId.Length > 0)
      {
        id = orgId;
      }

      if (EntraAzJson.TryReadStringProperty(org, "displayName", out string name) && name.Length > 0)
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

      if (!EntraAzJson.TryReadStringProperty(document.RootElement, "accessToken", out string token)
        || token.Length == 0)
      {
        return false;
      }

      accessToken = token;
      return true;
    }
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

      if (!EntraAzJson.TryReadStringProperty(domain, "name", out string name) || name.Length == 0)
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
          if (EntraAzJson.TryReadStringProperty(element, "tenantId", out string tenantId)
            && tenantId.Length > 0)
          {
            list.Add(tenantId.Trim());
          }

          continue;
        }

        if (EntraAzJson.TryReadStringProperty(element, "tenantId", out string fromTenantId)
          && fromTenantId.Length > 0)
        {
          list.Add(fromTenantId.Trim());
        }
        else if (EntraAzJson.TryReadStringProperty(element, "id", out string fromId) && fromId.Length > 0)
        {
          list.Add(fromId.Trim());
        }
      }
    }

    return true;
  }
}
