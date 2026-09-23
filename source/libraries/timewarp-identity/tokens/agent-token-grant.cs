#region Purpose
// A validated agent access token's claims: who it belongs to, which credential minted it, what it authorizes, and when it stops.
#endregion

#region Design
// Record, not a class with a private ctor + factory (unlike WebAuthnRegistrationResult/
// AgentKeyProofResult): a grant has no invalid-state to guard against — any (PrincipalId,
// non-null Scopes, ExpiresAt) combination is a legitimate value the store can return, so there is no
// invariant a private constructor would be protecting. IAgentTokenStore.Validate returns this or
// null uniformly (see that port's Design region) — never an exception, never a distinguishable
// "expired" vs "unknown token" result.
// CredentialId (task 248-002): the agent key that proved possession at issuance, carried so the
// per-request bearer validator can stamp Credential.LastUsedAt (coalesced —
// CredentialUsageRecorder). It is NOT a claim on the ticket and is never re-verified at
// validation time; revoking the key does not invalidate already-issued tokens (unchanged
// posture — tokens are short-lived and re-issuance requires a fresh proof).
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Claims from a validated opaque agent access token: subject, scopes, and absolute expiry.
/// </summary>
/// <param name="PrincipalId">Principal the token authorizes.</param>
/// <param name="CredentialId">Agent-key credential that proved possession at issuance.</param>
/// <param name="Scopes">Scope strings minted with the token.</param>
/// <param name="ExpiresAt">UTC instant after which <see cref="IAgentTokenStore.Validate"/> returns null.</param>
public sealed record AgentTokenGrant(
  /// <summary>Principal the token authorizes.</summary>
  PrincipalId PrincipalId,
  /// <summary>Agent-key credential that proved possession at issuance.</summary>
  CredentialId CredentialId,
  /// <summary>Scope strings minted with the token.</summary>
  IReadOnlyList<string> Scopes,
  /// <summary>UTC instant after which validation fails uniformly.</summary>
  DateTimeOffset ExpiresAt);
