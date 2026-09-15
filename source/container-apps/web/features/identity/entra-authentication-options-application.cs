#region Purpose
// Bound Authentication:Entra section: named-scheme enablement, OIDC authority, public callback origin, trusted tenants, bootstrap gate.
#endregion

#region Design
// RFC 219 D10 config sketch. Section name is Authentication:Entra (not the type name) so the bind
// is explicit in Program — do not rely on AddFluentValidatedOptions' type-name default.
// Enabled is also set from the obsolete Authentication:UseEntra synonym at registration; that
// synonym never selects Entra as DefaultScheme. AllowBootstrap gates Principal.Create only;
// sync-hit of an active EntraAccount still issues a session when the tenant is trusted.
// TrustedTenants are GUID strings; empty list means no tenant may bootstrap or sync-hit.
// PublicOrigin is the browser-facing origin when Web.Server sits behind a proxy that forwards
// over http (YARP http://_http.web-server, ACA ingress). Unset keeps request-derived redirect_uri
// (direct https://localhost:63611). Do not default from Ingress:PublicUrl — that is a dashboard
// display URL and can differ from the origin in use (63610 vs shared host). Do not consume
// X-Forwarded-* (task 104-031).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Diagnostics.CodeAnalysis;

public sealed class EntraAuthenticationOptions
{
  public const string SectionKey = "Authentication:Entra";

  public bool Enabled { get; set; }
  public string Instance { get; set; } = "https://login.microsoftonline.com/";
  public string TenantId { get; set; } = null!;
  public string ClientId { get; set; } = null!;
  public string? ClientSecret { get; set; }
  public string CallbackPath { get; set; } = "/signin-oidc";
  public List<string> TrustedTenants { get; set; } = [];
  public bool AllowBootstrap { get; set; }

  /// <summary>
  /// Browser-facing origin for the OIDC <c>redirect_uri</c> when Web.Server is behind a proxy.
  /// Example: <c>https://arch.timewarp.work</c>. Empty keeps request-derived behaviour.
  /// </summary>
  public string? PublicOrigin { get; set; }

  /// <summary>
  /// True when Enabled was derived from the obsolete Authentication:UseEntra synonym.
  /// Not a config key — set at registration so a one-version obsolete log can fire.
  /// </summary>
  public bool UsedObsoleteUseEntraKey { get; set; }

  [SuppressMessage(
    "Design",
    "CA1054:URI-like parameters should not be strings",
    Justification = "OpenIdConnectMessage.RedirectUri is a string; PublicOrigin is concatenated with CallbackPath as that protocol value.")]
  public bool TryGetPublicRedirectUri([NotNullWhen(true)] out string? redirectUri)
  {
    redirectUri = null;
    if (string.IsNullOrWhiteSpace(PublicOrigin))
    {
      return false;
    }

    string origin = PublicOrigin.Trim().TrimEnd('/');
    string callback = string.IsNullOrWhiteSpace(CallbackPath) ? "/signin-oidc" : CallbackPath.Trim();
    if (!callback.StartsWith('/'))
    {
      callback = "/" + callback;
    }

    redirectUri = origin + callback;
    return true;
  }
}
