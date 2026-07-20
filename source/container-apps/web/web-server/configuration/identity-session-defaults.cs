#region Purpose
// Constants for the browser session cookie scheme used by the passkey identity feature.
#endregion

#region Design
// Scheme is a NAMED authentication scheme, deliberately not registered as the default: the
// container already carries a dormant Microsoft Entra (Azure AD B2C) registration
// (AddMicrosoftIdentityWebAppAuthentication in Program.ConfigureAuthentication) that owns whatever
// the default scheme currently is. Adding this as an additional named scheme via a second
// AddAuthentication() call lets both coexist — CookieBrowserSessionService always signs in/reads by
// this explicit scheme name, never relying on "the default."
// AuthenticatedPolicy (task 110): "any signed-in identity-session cookie" — scheme-restricted
// (AddAuthenticationSchemes(Scheme)) + RequireAuthenticatedUser(), no further policy shape. This is
// the policy the admin Roles CRUD contracts carry via [EndpointAuthorize(Policy="…")] (the contract
// uses the raw string literal — web-contracts cannot reference web-server's constant, the
// dependency runs the other way — with a comment naming this constant as the source of truth).
// Deliberately NOT an admin/role-based policy: no admin/role authorization model exists in the
// template yet (recorded future work, task 110 scope boundary) — "any authenticated principal may
// mutate demo roles" is a known, deliberate simplification for a template whose job is teaching the
// protected-endpoint PATTERN, not shipping real admin authorization.
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class IdentitySessionDefaults
{
  public const string Scheme = "identity-session";
  public const string CookieName = ".timewarp.identity.session";
  public const string PrincipalIdClaimType = "timewarp:principal_id";
  public const string AuthenticatedPolicy = "identity-session-authenticated";
}
