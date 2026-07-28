#region Purpose
// Port for reading the current request's authenticated agent-token caller so identity handlers
// never depend on ASP.NET Core's HttpContext/ClaimsPrincipal directly — the bearer-token analog of
// IBrowserSessionService.
#endregion

#region Design
// Synchronous, unlike IBrowserSessionService (async SignInAsync/AuthenticateAsync): agent-token
// authentication is per-request bearer validation performed by the ASP.NET Core authentication
// pipeline BEFORE the handler ever runs (AgentTokenAuthenticationHandler, web-server) — by the time
// a handler calls GetCurrentCaller, the claims are already sitting on HttpContext.User; there is no
// I/O left to await, only claim reads. Only get-agent-identity-handler.cs uses this today, but the
// port stays HttpContext-free by design (mirrors IBrowserSessionService), so a future handler never
// needs to know ASP.NET Core exists.
// Returns null (never throws) when the caller is not an authenticated agent-token principal — the
// consuming handler treats this as a defense-in-depth 401 even though [Authorize] on the endpoint
// should make the null case unreachable in practice.
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IAgentCallerContext
{
  AgentCaller? GetCurrentCaller();
}

/// <summary>The authenticated agent principal and the scopes carried by the presented bearer token.</summary>
public sealed record AgentCaller(PrincipalId PrincipalId, IReadOnlyList<string> Scopes);
