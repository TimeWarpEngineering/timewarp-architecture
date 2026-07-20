#region Purpose
// Port for reading the current request's caller principal id regardless of which authentication
// scheme authenticated it — the scheme-agnostic counterpart to IBrowserSessionService (cookie-only)
// and IAgentCallerContext (agent-token-only), for surfaces a caller can reach via EITHER scheme.
#endregion

#region Design
// Task 104-005: the credential-management surface (list/add/revoke) accepts either the
// identity-session cookie OR an agent bearer token carrying credential:manage — see
// CredentialManagementDefaults' Design region for the policy shape. Handlers on that surface must
// resolve "whose credentials" from whichever scheme actually authenticated the request, without
// branching on cookie-vs-bearer themselves.
// Why not reuse IBrowserSessionService.GetCurrentPrincipalIdAsync or IAgentCallerContext.
// GetCurrentCaller: both are deliberately SINGLE-scheme ports (the former calls
// HttpContext.AuthenticateAsync against the identity-session scheme by name; the latter checks
// AuthenticationType == agent-token before trusting the claims) — correct for their own
// single-scheme endpoints, but neither can serve a policy that accepts either scheme without the
// caller trying one, catching the miss, and trying the other. This port instead reads
// HttpContext.User directly: for a policy built with AddAuthenticationSchemes(schemeA, schemeB),
// ASP.NET Core's authorization middleware already ran authentication against whichever scheme
// matched and reassigned HttpContext.User to that scheme's ClaimsPrincipal BEFORE any handler runs —
// by the time this port is called, "which scheme won" is no longer a question that needs asking.
// Both schemes write the caller's principal id under the SAME claim type
// (IdentitySessionDefaults.PrincipalIdClaimType — AgentTokenDefaults' own Design region documents
// that this is a deliberate shared claim type, not a coincidence), which is what makes one
// implementation correct for both schemes.
// Async signature (Task<PrincipalId?>) matches IBrowserSessionService for call-site consistency
// (handlers already `await` a caller-resolution port) even though the web-server implementation
// performs no I/O — a synchronous claim read wrapped in Task.FromResult, mirroring
// IAgentCallerContext's reasoning for why ITS synchronous signature needs no awaiting (the claims
// are already sitting on HttpContext.User), just expressed as async for uniformity with the other
// port instead of adding a third, sync-only shape to the abstraction set.
// Returns null (never throws) when there is no authenticated caller or the claim is missing/
// unparsable — callers treat this as a defense-in-depth 401 even though the policy's
// RequireAuthenticatedUser() should make the null case unreachable in practice (same posture as
// IAgentCallerContext's Design region).
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface ICurrentPrincipalAccessor
{
  Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken);
}
