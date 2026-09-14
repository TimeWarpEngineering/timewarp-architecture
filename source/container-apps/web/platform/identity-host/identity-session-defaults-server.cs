#region Purpose
// Constants for the browser session cookie scheme used by the passkey identity feature.
#endregion

#region Design
// RFC 219 D10: this scheme is always DefaultScheme. Entra is a named OIDC scheme ("entra") and
// never owns the default (the 104-021 UseEntra branch that called
// AddMicrosoftIdentityWebAppAuthentication is deleted). CookieBrowserSessionService always signs
// in/reads by this explicit scheme name, never relying on "the default."
// AuthenticatedPolicy (task 110): "any signed-in identity-session cookie" — scheme-restricted
// (AddAuthenticationSchemes(Scheme)) + RequireAuthenticatedUser(), no further policy shape. Used by
// non-admin identity-session-gated surfaces. Admin Roles/Principals APIs (task 182-002) use
// PermissionIds policies (admin.roles.* / admin.principals.*) via PermissionRequirementHandler
// instead of this any-authenticated policy.
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class IdentitySessionDefaults
{
  public const string Scheme = "identity-session";
  public const string CookieName = ".timewarp.identity.session";
  public const string PrincipalIdClaimType = "timewarp:principal_id";
  public const string AuthenticatedPolicy = "identity-session-authenticated";
}
