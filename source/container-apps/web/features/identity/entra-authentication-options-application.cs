#region Purpose
// Bound Authentication:Entra section: named-scheme enablement, OIDC authority, trusted tenants, bootstrap gate.
#endregion

#region Design
// RFC 219 D10 config sketch. Section name is Authentication:Entra (not the type name) so the bind
// is explicit in Program — do not rely on AddFluentValidatedOptions' type-name default.
// Enabled is also set from the obsolete Authentication:UseEntra synonym at registration; that
// synonym never selects Entra as DefaultScheme. AllowBootstrap gates Principal.Create only;
// sync-hit of an active EntraAccount still issues a session when the tenant is trusted.
// TrustedTenants are GUID strings; empty list means no tenant may bootstrap or sync-hit.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

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
  /// True when Enabled was derived from the obsolete Authentication:UseEntra synonym.
  /// Not a config key — set at registration so a one-version obsolete log can fire.
  /// </summary>
  public bool UsedObsoleteUseEntraKey { get; set; }
}
