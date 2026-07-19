#region Purpose
// Port for issuing and validating opaque scoped agent access tokens — hosts supply the
// implementation (in-memory here; a distributed store is out of scope for this task).
#endregion

#region Design
// Opaque, store-backed tokens — NOT JWT (task 104-004 §2). JwtBearer is not in the ASP.NET Core
// shared framework (verified vs Microsoft.AspNetCore.App.Ref 10.0.10; only BearerToken ships
// in-box), and a JWT would cost a package plus signing-key management for zero Wave-1 benefit (one
// validating host, no third-party audience). Opaque tokens are LIVE-REVOCABLE: Validate re-reads the
// principal on every call, so a quarantine takes effect immediately for every already-issued token —
// a JWT would remain valid (its claims self-contained, unchecked against current principal state)
// until natural expiry. x402/quota work (104-013) needs this same live-check property at settle
// time. Revisit trigger: a multi-instance host or a third-party token validator would need a
// distributed store or signed tokens at the host layer — this port's shape (Issue/Validate) does not
// change either way, only the implementation behind it.
// Expiry is fixed per issuance (15 min default, configurable via AgentTokenOptions), no refresh
// tokens: refresh IS the token ceremony — an agent re-signs a fresh challenge with the same key to
// mint a new token. A refresh-token concept would duplicate that capability for no Wave-1 benefit.
// Validate returns null uniformly for unknown, garbage, or expired tokens — never distinguishes
// these (mirrors IWebAuthnChallengeStore/IAgentKeyChallengeStore's uniform-false-on-failure
// contract) — a caller (the bearer authentication handler) that could tell "expired" from "never
// issued" would leak token-lifecycle information to a caller presenting a guessed/garbage token.
#endregion

namespace TimeWarp.Identity;

public interface IAgentTokenStore
{
  /// <summary>Mints a new opaque bearer token for the given principal/scopes, valid for the given lifetime.</summary>
  string Issue(PrincipalId principalId, IReadOnlyCollection<string> scopes, TimeSpan lifetime);

  /// <summary>
  /// Validates a presented bearer token. Returns the grant if it exists and has not expired;
  /// returns null uniformly for unknown, malformed, or expired tokens.
  /// </summary>
  AgentTokenGrant? Validate(string token);
}
