#region Purpose
// Why an OpenID issuer string is not an Entra v2 tenant issuer.
#endregion

#region Design
// Distinct from EntraTenantLookupOutcome. The lookup can fail before an issuer exists
// (network, timeout, HTTP status). This enum is only the issuer-string check.
#endregion

namespace TimeWarp.Identity;

/// <summary>Why <see cref="EntraIssuer.TryParse"/> rejected an issuer.</summary>
public enum EntraIssuerParseFailure
{
  /// <summary>The issuer parsed.</summary>
  None = 0,

  /// <summary>The discovery document had no issuer string.</summary>
  Missing,

  /// <summary>The issuer is not an absolute https URI with path <c>/{guid}/v2.0</c>.</summary>
  Malformed,

  /// <summary>The path segment is not a non-empty GUID.</summary>
  NotAGuid,

  /// <summary>The host is not <c>login.microsoftonline.com</c>.</summary>
  WrongHost,
}
