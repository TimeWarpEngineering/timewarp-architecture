#region Purpose
// Readers for `az` JSON output (account, app list, app show, string arrays).
#endregion

#region Design
// az JSON is parsed with JsonDocument (AOT-safe; no reflection serializer). TryParseObject and
// TryReadStringProperty are the shared low-level readers; EntraTenantJson reuses them.
#endregion

namespace DevCli.Services;

using System.Text.Json;

internal static class EntraAzJson
{
  internal static bool TryReadAccount(string json, out string tenantId, out string user)
  {
    tenantId = "";
    user = "";
    if (!TryParseObject(json, out JsonDocument document))
    {
      return false;
    }

    using (document)
    {
      JsonElement root = document.RootElement;
      if (!TryReadStringProperty(root, "tenantId", out tenantId) || tenantId.Length == 0)
      {
        return false;
      }

      TryReadStringProperty(root, "user", out user);
      return true;
    }
  }

  internal static bool TryFindExactAppIds(
    string listJson,
    string displayName,
    out IReadOnlyList<string> appIds)
  {
    List<string> ids = [];
    appIds = ids;
    if (string.IsNullOrWhiteSpace(listJson))
    {
      return true;
    }

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(listJson);
    }
    catch (JsonException)
    {
      return false;
    }

    using (document)
    {
      if (document.RootElement.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      foreach (JsonElement app in document.RootElement.EnumerateArray())
      {
        if (app.ValueKind != JsonValueKind.Object)
        {
          continue;
        }

        if (!TryReadStringProperty(app, "displayName", out string name)
          || !TryReadStringProperty(app, "appId", out string appId))
        {
          continue;
        }

        if (string.Equals(name, displayName, StringComparison.OrdinalIgnoreCase)
          && appId.Length > 0)
        {
          ids.Add(appId);
        }
      }
    }

    return true;
  }

  internal static bool TryReadAppShow(
    string json,
    out string appId,
    out string displayName,
    out IReadOnlyList<string> redirectUris)
  {
    appId = "";
    displayName = "";
    redirectUris = [];
    if (!TryParseObject(json, out JsonDocument document))
    {
      return false;
    }

    using (document)
    {
      JsonElement root = document.RootElement;
      TryReadStringProperty(root, "appId", out appId);
      TryReadStringProperty(root, "displayName", out displayName);
      if (root.TryGetProperty("redirectUris", out JsonElement urisElement))
      {
        if (urisElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
          return appId.Length > 0;
        }

        if (!TryReadStringArray(urisElement.GetRawText(), out redirectUris))
        {
          redirectUris = [];
        }
      }

      return appId.Length > 0;
    }
  }

  internal static bool TryReadStringArray(string json, out IReadOnlyList<string> values)
  {
    List<string> list = [];
    values = list;
    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
    {
      return true;
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
      if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
      {
        return true;
      }

      if (root.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      foreach (JsonElement element in root.EnumerateArray())
      {
        if (element.ValueKind != JsonValueKind.String)
        {
          continue;
        }

        string? value = element.GetString();
        if (!string.IsNullOrWhiteSpace(value))
        {
          list.Add(value);
        }
      }
    }

    return true;
  }

  internal static bool TryParseObject(string json, out JsonDocument document)
  {
    document = null!;
    if (string.IsNullOrWhiteSpace(json))
    {
      return false;
    }

    try
    {
      document = JsonDocument.Parse(json);
    }
    catch (JsonException)
    {
      return false;
    }

    if (document.RootElement.ValueKind != JsonValueKind.Object)
    {
      document.Dispose();
      document = null!;
      return false;
    }

    return true;
  }

  internal static bool TryReadStringProperty(JsonElement element, string name, out string value)
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
