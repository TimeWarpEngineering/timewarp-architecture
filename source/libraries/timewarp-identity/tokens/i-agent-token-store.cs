#region Purpose
// Port for issuing and validating opaque scoped agent access tokens — hosts supply the
// implementation (in-memory here; a distributed store is out of scope for this task).
#endregion

#region Design
// Opaque, store-backed tokens — NOT JWT (task 104-004 §2). JwtBearer is not in the ASP.NET Core
// shared framework (verified vs Microsoft.AspNetCore.App.Ref 10.0.10; only BearerToken ships
// in-box), and a JWT would cost a package plus signing-key management for zero Wave-1 benefit (one
// validating host, no third-party audience).
// Scope correction (round-1 finding M2): THIS PORT knows ONLY expiry and the grant it stored at
// Issue time — Validate does NOT re-read the principal, does NOT know about quarantine, and has no
// IPrincipalStore dependency at all (by design: a token store should not need to know what a
// principal even is). An earlier version of this region claimed "Validate re-reads the principal on
// every call" — that was never true of this type; the actual principal-liveness re-read that makes
// opaque tokens live-revocable happens ONE layer up, in the CALLER
// (AgentTokenAuthenticationHandler.HandleAuthenticateAsync, web-server) — it calls
// IPrincipalStore.GetPrincipalAsync(grant.PrincipalId) and checks principal.IsActive AFTER a
// successful Validate, and THAT check is what delivers the immediate quarantine cutoff a JWT could
// not offer (a JWT's claims are self-contained and unchecked against current principal state until
// natural expiry).
// Consequence for future callers, spelled out because the wrong claim was load-bearing: ANY new
// caller of IAgentTokenStore.Validate — most concretely 104-013's x402/quota settle-time check, or a
// future api-server bearer validator — MUST independently re-read the principal and check liveness
// itself; calling Validate alone gets ONLY "does this token exist and has it not expired," never a
// quarantine cutoff. Do not assume this port enforces it. If a future task finds itself repeating
// that liveness check at every new callsite, the stronger fix is a port operation that folds the
// liveness check into the seam itself (so it cannot be forgotten) — not implemented here, Wave 1
// keeps the port principal-agnostic and pushes the obligation to callers explicitly.
// Revisit trigger: a multi-instance host or a third-party token validator would need a distributed
// store or signed tokens at the host layer — this port's shape (Issue/Validate) does not change
// either way, only the implementation behind it.
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
  /// returns null uniformly for unknown, malformed, or expired tokens. Does NOT check principal
  /// liveness/quarantine — this store has no IPrincipalStore access. Callers enforcing quarantine
  /// (today: AgentTokenAuthenticationHandler) MUST independently re-read the principal after a
  /// successful Validate; see this interface's Design region.
  /// </summary>
  AgentTokenGrant? Validate(string token);
}
