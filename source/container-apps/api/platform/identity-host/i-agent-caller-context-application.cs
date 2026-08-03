#region Purpose
// Port for reading the current request's authenticated agent-token caller so application handlers
// never depend on ASP.NET Core's HttpContext/ClaimsPrincipal directly.
#endregion

#region Design
// Synchronous: agent-token authentication is per-request bearer validation performed by the ASP.NET
// Core pipeline BEFORE the handler runs (AgentTokenAuthenticationHandler) — by the time a handler
// calls GetCurrentCaller, claims are already on HttpContext.User; only claim reads remain.
// Returns null (never throws) when the caller is not an authenticated agent-token principal —
// consuming handlers treat this as defense-in-depth 401 even though [EndpointAuthorize] should make
// the null case unreachable.
// Parity with web's IAgentCallerContext (platform/identity-host); api-server cannot reference
// web assemblies so the port is declared here for the api family.
#endregion

namespace TimeWarp.Architecture.Abstractions;

using TimeWarp.Identity;

public interface IAgentCallerContext
{
  AgentCaller? GetCurrentCaller();
}

/// <summary>The authenticated agent principal and the scopes carried by the presented bearer token.</summary>
public sealed record AgentCaller(PrincipalId PrincipalId, IReadOnlyList<string> Scopes);
