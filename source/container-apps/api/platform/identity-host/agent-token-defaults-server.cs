#region Purpose
// Constants for the bearer-token authentication scheme used by agent-facing routes on api-server.
#endregion

#region Design
// Values MUST stay byte-identical to web-server's AgentTokenDefaults (platform/identity-host on web)
// and IdentitySessionDefaults.PrincipalIdClaimType — a token minted against one host's claim types
// must be readable by the other if a shared IAgentTokenStore is ever wired (Redis). Scheme is a
// NAMED authentication scheme (not the default): api-server has no cookie/Entra default to coexist
// with today, but the scheme name still gates policies via AddAuthenticationSchemes so a future
// second scheme cannot accidentally satisfy agent-scope policies.
// DemoInvokePolicy is registered for parity with web (related scope policies) even though the
// api sample endpoint only exercises IdentityReadPolicy.
// Duplication note (task 104-030): the handler + defaults live under api/platform because
// web-server assemblies are not referenced by api-server (separate deployables). Behavior and
// string constants track web's AgentTokenAuthenticationHandler / AgentTokenDefaults — change both
// when changing claim types, scheme name, or policy names.
#endregion

namespace TimeWarp.Architecture.Configuration;

using TimeWarp.Identity;

public static class AgentTokenDefaults
{
  public const string Scheme = "agent-token";
  public const string ScopeClaimType = "timewarp:scope";
  /// <summary>Same claim type web uses on IdentitySessionDefaults — principal id is one concept.</summary>
  public const string PrincipalIdClaimType = "timewarp:principal_id";
  public const string IdentityReadPolicy = "agent-scope:identity:read";
  /// <summary>Bearer policy requiring <see cref="AgentScopes.DemoInvoke"/> (parity with web, 104-011).</summary>
  public const string DemoInvokePolicy = "agent-scope:demo:invoke";
}
