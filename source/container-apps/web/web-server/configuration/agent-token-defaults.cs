#region Purpose
// Constants for the bearer-token authentication scheme used by the agent identity feature.
#endregion

#region Design
// Scheme is a NAMED authentication scheme, deliberately not registered as the default and not
// touching the identity-session cookie scheme (104-003) or the dormant Microsoft Entra registration
// (lock #10) — added via the SAME AddAuthentication() chain that already registers the cookie
// scheme, one more .AddScheme(...) link, so all three coexist. AgentTokenAuthenticationHandler
// always authenticates/challenges by this explicit scheme name, never relying on "the default," and
// IdentityReadPolicy restricts its AuthenticationSchemes to ONLY this scheme — a request carrying
// nothing but a valid identity-session cookie is never even offered to this scheme's handler, which
// is what makes scheme isolation structural rather than incidental (see
// AgentTokenAuthenticationHandler's Design region and Agent_Protected_Endpoint_Tests's
// cookie-session-cannot-reach-bearer-endpoint case).
// ScopeClaimType is a DIFFERENT claim type than IdentitySessionDefaults.PrincipalIdClaimType (which
// IS reused here for the principal-id claim — same identity concept, same claim type, deliberately
// shared) — a ClaimsPrincipal from either scheme therefore carries the principal id under one
// consistent claim type, while scope claims (agent-token-specific, no cookie-scheme equivalent) get
// their own type.
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class AgentTokenDefaults
{
  public const string Scheme = "agent-token";
  public const string ScopeClaimType = "timewarp:scope";
  public const string IdentityReadPolicy = "agent-scope:identity:read";
}
