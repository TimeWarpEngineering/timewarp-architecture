#region Purpose
// Parse a tenant id from an Entra v2 OpenID issuer URL.
#endregion

#region Design
// The only accepted issuer is https://login.microsoftonline.com/{guid}/v2.0.
// https, host login.microsoftonline.com (case-insensitive), no userinfo, no non-default port,
// no query, no fragment, path exactly /{guid}/v2.0, and the GUID is not empty.
// Check order: not an absolute https URI is Malformed; a different host, userinfo, or port is
// WrongHost; a path that is not /{segment}/v2.0, or a query or fragment, is Malformed; a segment
// that is not a non-empty GUID is NotAGuid. Missing is reserved for a discovery document with no
// issuer property. Callers that only have a string use Malformed for blank input.
#endregion

namespace TimeWarp.Identity;

/// <summary>Entra v2 issuer URL rules for tenant-id discovery.</summary>
public static class EntraIssuer
{
  /// <summary>Host of a v2 issuer.</summary>
  public const string Host = "login.microsoftonline.com";

  /// <summary>
  /// Reads the tenant id from <paramref name="issuer"/>.
  /// Blank input is <see cref="EntraIssuerParseFailure.Malformed"/>.
  /// </summary>
  public static bool TryParse(string? issuer, out Guid tenantId, out EntraIssuerParseFailure failure)
  {
    tenantId = Guid.Empty;
    if (string.IsNullOrWhiteSpace(issuer))
    {
      failure = EntraIssuerParseFailure.Malformed;
      return false;
    }

    if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
    {
      failure = EntraIssuerParseFailure.Malformed;
      return false;
    }

    if (!string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase)
      || !string.IsNullOrEmpty(uri.UserInfo)
      || uri.Port != 443)
    {
      failure = EntraIssuerParseFailure.WrongHost;
      return false;
    }

    if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
    {
      failure = EntraIssuerParseFailure.Malformed;
      return false;
    }

    string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length != 2 || !string.Equals(segments[1], "v2.0", StringComparison.Ordinal))
    {
      failure = EntraIssuerParseFailure.Malformed;
      return false;
    }

    if (!Guid.TryParse(segments[0], out Guid parsed) || parsed == Guid.Empty)
    {
      failure = EntraIssuerParseFailure.NotAGuid;
      return false;
    }

    tenantId = parsed;
    failure = EntraIssuerParseFailure.None;
    return true;
  }
}
