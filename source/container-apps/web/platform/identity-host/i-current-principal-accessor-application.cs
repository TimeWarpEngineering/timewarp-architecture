#region Purpose
// Port for reading the current request's caller principal id regardless of which authentication
// scheme authenticated it — the scheme-agnostic counterpart to IBrowserSessionService (cookie-only)
// and IAgentCallerContext (agent-token-only), for surfaces a caller can reach via EITHER scheme.
#endregion

#region Design
// Task 104-005 / 182-006: the credential surface (list/add/revoke) accepts either the
// identity-session cookie OR an agent bearer token (PermissionIds.CredentialManageSelf + dual
// AuthenticationSchemes on contracts; agents need scope credential:manage expanded by the
// evaluator). Handlers resolve "whose credentials" from whichever scheme authenticated the
// request, without branching on cookie-vs-bearer themselves.
// Why not reuse IBrowserSessionService.GetCurrentPrincipalIdAsync or IAgentCallerContext.
// GetCurrentCaller: both are deliberately SINGLE-scheme ports (the former calls
// HttpContext.AuthenticateAsync against the identity-session scheme by name; the latter checks
// AuthenticationType == agent-token before trusting the claims) — correct for their own
// single-scheme endpoints, but neither can serve a policy that accepts either scheme without the
// caller trying one, catching the miss, and trying the other. This port instead reads
// HttpContext.User directly.
// Round-1 review (M1, security-confirmed no risk): when the combined authorization policy lists
// schemeA and schemeB (from the named policy's AddAuthenticationSchemes and/or FastEndpoints
// AuthSchemes from [EndpointAuthorize(AuthenticationSchemes)]), ASP.NET Core's authorization
// middleware authenticates against EVERY listed scheme that the request carries credentials for,
// and MERGES every successfully-authenticated identity onto HttpContext.User (one ClaimsIdentity
// per scheme that succeeded) — it does not pick a single "winning" scheme and discard the rest.
// In the ordinary case exactly one scheme succeeds (a browser presents the identity-session cookie;
// an agent presents a bearer token; essentially never both), so HttpContext.User carries exactly
// one identity and FindFirstValue's result is unambiguous. If a caller presents BOTH a valid cookie AND
// a valid bearer token on the same request, both succeed and both identities are merged;
// FindFirstValue then returns the principal-id claim from whichever identity happens first in
// merge order, not a deliberately resolved "the real caller." This is deliberately NOT hardened
// into a defined precedence, because it does not need to be: resolving to EITHER identity is a
// legitimate self-scope either way — the request demonstrably controls both credentials (a stolen
// cookie or bearer token alone cannot forge this, since each is independently verified by its own
// scheme's handler before either identity is merged), so worst case this handler operates on the
// caller's OTHER own principal, never a principal the caller does not control. Fails safe by
// construction, not by the specific merge order ASP.NET Core happens to use.
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
