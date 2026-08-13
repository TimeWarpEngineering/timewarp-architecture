#region Purpose
// Constants for the bearer-token authentication scheme used by the agent identity feature.
#endregion

#region Design
// Scheme is a NAMED authentication scheme, deliberately not registered as the default and not
// touching the identity-session cookie scheme (104-003) or the dormant Microsoft Entra registration
// (lock #10) — added via the SAME AddAuthentication() chain that already registers the cookie
// scheme, one more .AddScheme(...) link, so all three coexist. AgentTokenAuthenticationHandler
// always authenticates/challenges by this explicit scheme name, never relying on "the default."
// Task 182-006: product policies are PermissionIds (identity.read, demo.invoke, …) registered via
// AddPermissionPolicies; agent-only routes declare AuthenticationSchemes = agent-token on
// [EndpointAuthorize] so a cookie-only request is never offered to this scheme (see
// Agent_Protected_Endpoint_Tests cookie-session-cannot-reach-bearer-endpoint). Scope claims expand
// to permissions in PermissionEvaluator via IAgentCallerContext + AgentScopePermissionSeed.
// ScopeClaimType is a DIFFERENT claim type than IdentitySessionDefaults.PrincipalIdClaimType
// (which IS reused here for the principal-id claim — same identity concept, same claim type,
// deliberately shared) — a ClaimsPrincipal from either scheme therefore carries the principal id
// under one consistent claim type, while scope claims (agent-token-specific) get their own type.
// IdentityReadPolicy / DemoInvokePolicy string constants are historical aliases (pre-permission
// policy names); prefer PermissionIds on web contracts. Api-server still registers claim-based
// policies under its own copy of these names until it adopts the same evaluator path.
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class AgentTokenDefaults
{
  public const string Scheme = "agent-token";
  public const string ScopeClaimType = "timewarp:scope";
  /// <summary>Historical policy name; prefer <c>PermissionIds.IdentityRead</c> on web contracts.</summary>
  public const string IdentityReadPolicy = "identity.read";
  /// <summary>Historical policy name; prefer <c>PermissionIds.DemoInvoke</c> on web contracts.</summary>
  public const string DemoInvokePolicy = "demo.invoke";
}
