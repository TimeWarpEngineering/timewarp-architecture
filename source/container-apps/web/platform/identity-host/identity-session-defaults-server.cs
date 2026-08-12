#region Purpose
// Constants for the browser session cookie scheme used by the passkey identity feature.
#endregion

#region Design
// Task 104-021: when Authentication:UseEntra is false (default), this scheme IS the authentication
// defaultScheme. When UseEntra is true, Entra owns the default and this remains a named scheme
// registered via a second AddAuthentication() — CookieBrowserSessionService always signs in/reads
// by this explicit scheme name, never relying on "the default," so both postures work.
// AuthenticatedPolicy (task 110): "any signed-in identity-session cookie" — scheme-restricted
// (AddAuthenticationSchemes(Scheme)) + RequireAuthenticatedUser(), no further policy shape. Used by
// non-admin identity-session-gated surfaces. Admin Roles/Principals APIs (task 147-004) now use
// AuthorizationPolicyNames.CanViewRolesPage / CanViewPrincipalsPage with RequireRole(Administrator)
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
