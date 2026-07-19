#region Purpose
// A validated agent access token's claims: who it belongs to, what it authorizes, and when it stops.
#endregion

#region Design
// Record, not a class with a private ctor + factory (unlike WebAuthnRegistrationResult/
// AgentKeyProofResult): a grant has no invalid-state to guard against — any (PrincipalId,
// non-null Scopes, ExpiresAt) combination is a legitimate value the store can return, so there is no
// invariant a private constructor would be protecting. IAgentTokenStore.Validate returns this or
// null uniformly (see that port's Design region) — never an exception, never a distinguishable
// "expired" vs "unknown token" result.
#endregion

namespace TimeWarp.Identity;

public sealed record AgentTokenGrant(PrincipalId PrincipalId, IReadOnlyList<string> Scopes, DateTimeOffset ExpiresAt);
