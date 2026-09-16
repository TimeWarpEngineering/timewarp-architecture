#region Purpose
// Pure helpers for `dev entra`: redirect-URI union, secret masking, user-secrets parse, az JSON.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Redirect-URI union is case-insensitive and preserves first-seen order (existing, then desired).
// Client secrets are never formatted into an invocation string; MaskSecret is the only display form.
// Mint only on --new-secret or a successful list with no ClientSecret; a failed list aborts
// (never fail-open mint — that would --append an Azure password that may never be stored).
// az JSON is parsed with JsonDocument (AOT-safe; no reflection serializer).
// TenantDisplayNameKey / TenantDomainKey are the SSOT strings for informational user-secrets
// written by setup and shown by status (see EntraTenants for formatting/selection).
#endregion

namespace DevCli.Services;

using System.Text;
using System.Text.Json;

internal static class EntraSetup
{
  internal const string DefaultDisplayName = "TimeWarp Architecture Dev";
  internal const string CallbackPath = "/signin-oidc";
  internal const string WebServerHttpsRedirect = "https://localhost:63611/signin-oidc";
  internal const string IngressHttpsRedirect = "https://localhost:63610/signin-oidc";
  internal const string WebServerProject =
    "source/container-apps/web/projects/web-server/web-server.csproj";
  internal const string MaskedSecret = "********";

  internal const string EnabledKey = "Authentication:Entra:Enabled";
  internal const string TenantIdKey = "Authentication:Entra:TenantId";
  internal const string TenantDisplayNameKey = "Authentication:Entra:TenantDisplayName";
  internal const string TenantDomainKey = "Authentication:Entra:TenantDomain";
  internal const string ClientIdKey = "Authentication:Entra:ClientId";
  internal const string ClientSecretKey = "Authentication:Entra:ClientSecret";
  internal const string TrustedTenants0Key = "Authentication:Entra:TrustedTenants:0";
  internal const string AllowBootstrapKey = "Authentication:Entra:AllowBootstrap";
  internal const string PublicOriginKey = "Authentication:Entra:PublicOrigin";

  internal static IReadOnlyList<string> BuildDesiredRedirectUris(
    string? publicOrigin,
    IReadOnlyList<string>? extraRedirectUris)
  {
    List<string> desired = [WebServerHttpsRedirect, IngressHttpsRedirect];
    if (!string.IsNullOrWhiteSpace(publicOrigin))
    {
      desired.Add(ToSignInOidcUri(publicOrigin));
    }

    if (extraRedirectUris is not null)
    {
      desired.AddRange(extraRedirectUris);
    }

    return UnionRedirectUris([], desired);
  }

  internal static string ToSignInOidcUri(string origin)
  {
    string trimmed = origin.Trim().TrimEnd('/');
    if (trimmed.EndsWith(CallbackPath, StringComparison.OrdinalIgnoreCase))
    {
      return trimmed;
    }

    return trimmed + CallbackPath;
  }

  internal static IReadOnlyList<string> UnionRedirectUris(
    IEnumerable<string> existing,
    IEnumerable<string> desired)
  {
    List<string> union = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    foreach (string uri in existing.Concat(desired))
    {
      string trimmed = uri.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      if (seen.Add(trimmed))
      {
        union.Add(trimmed);
      }
    }

    return union;
  }

  internal static bool RedirectUrisEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
  {
    if (left.Count != right.Count)
    {
      return false;
    }

    HashSet<string> leftSet = new(left, StringComparer.OrdinalIgnoreCase);
    foreach (string uri in right)
    {
      if (!leftSet.Contains(uri))
      {
        return false;
      }
    }

    return true;
  }

  internal static string MaskSecret(string? secret)
  {
    return string.IsNullOrEmpty(secret) ? "(not set)" : MaskedSecret;
  }

  internal static string CredentialDisplayName(DateTimeOffset timestamp) =>
    $"dev-{timestamp.UtcDateTime:yyyyMMdd}";

  internal static string FormatInvocation(string executable, IReadOnlyList<string> arguments)
  {
    StringBuilder builder = new();
    builder.Append(executable);
    foreach (string argument in arguments)
    {
      builder.Append(' ');
      builder.Append(QuoteArgument(argument));
    }

    return builder.ToString();
  }

  internal static Dictionary<string, string> ParseUserSecretsList(string stdout)
  {
    Dictionary<string, string> secrets = new(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrWhiteSpace(stdout))
    {
      return secrets;
    }

    foreach (string line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
    {
      int separator = line.IndexOf(" = ", StringComparison.Ordinal);
      if (separator <= 0)
      {
        continue;
      }

      string key = line[..separator].Trim();
      string value = line[(separator + 3)..];
      if (key.Length == 0)
      {
        continue;
      }

      secrets[key] = value;
    }

    return secrets;
  }

  internal static bool HasClientSecret(IReadOnlyDictionary<string, string> secrets)
  {
    return secrets.TryGetValue(ClientSecretKey, out string? value)
      && !string.IsNullOrWhiteSpace(value);
  }

  internal static bool TryDecideMintClientSecret(
    bool newSecret,
    bool listSucceeded,
    bool hasExistingClientSecret,
    out bool mint)
  {
    if (newSecret)
    {
      mint = true;
      return true;
    }

    if (!listSucceeded)
    {
      mint = false;
      return false;
    }

    mint = !hasExistingClientSecret;
    return true;
  }

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

  private static string QuoteArgument(string argument)
  {
    if (argument.Length == 0 || argument.Any(static ch => char.IsWhiteSpace(ch) || ch is '"' or '\''))
    {
      return "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    return argument;
  }

  private static bool TryParseObject(string json, out JsonDocument document)
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
