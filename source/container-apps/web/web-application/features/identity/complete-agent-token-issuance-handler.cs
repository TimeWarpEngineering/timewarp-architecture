#region Purpose
// Server-side handler for the CompleteAgentTokenIssuance command: verifies the agent's proof of
// possession for a previously-registered key and mints a scoped bearer access token on success.
#endregion

#region Design
// Order is deliberate and replay-safety-critical, mirroring CompletePasskeyAuthentication.Handler:
// decode -> consume the challenge -> validate scope shape -> look up the credential/principal ->
// verify -> quarantine check -> issue. Consuming the challenge BEFORE verification means a
// tampered/replayed payload can never retry the same challenge, even when the request fails for an
// unrelated reason (e.g. an unknown scope) later in the same handler call.
// Scope validation (unknown scope -> 400 invalid_scope, listing the offending names) runs BEFORE the
// credential lookup: this is a REQUEST-shape check, not an account-disclosure risk — scope names are
// public, well-known constants (AgentScopes), so echoing back which ones were not recognized
// discloses nothing about any principal/credential and does not need the no-enumeration-oracle
// posture the credential/signature checks below require.
// No-enumeration-oracle posture (matches CompletePasskeyAuthentication.Handler exactly): an unknown
// KeyId, a revoked credential, a missing principal, and a bad signature all return the SAME generic
// 400 "Token issuance failed" — an attacker probing the endpoint cannot distinguish "this key was
// never registered" from "this key exists but is revoked" from "the signature does not match."
// Quarantine is checked ONLY AFTER AgentKeyProof.Verify succeeds (403, a distinct signal) — mirrors
// CompletePasskeyAuthentication.Handler's corrected ordering (104-003 round-1 finding M2: an earlier
// version of that sibling handler checked quarantine BEFORE verification, letting a caller who only
// KNEW a valid KeyId — no private key required — learn "quarantined" vs "active" as a pre-auth
// oracle). Checking quarantine post-Verify makes "the caller has already cryptographically proven
// possession" true before the distinguishable 403 is ever reachable.
// At bearer VALIDATION time (not issuance), quarantine is a SILENT Fail -> 401, not a 403 — see
// AgentTokenAuthenticationHandler's Design region (web-server) for that mapping. The two are
// deliberately different: issuance is a fresh proof-of-possession ceremony (403 discloses nothing
// new, possession is already proven), whereas presenting an old bearer token is not a fresh
// proof — a distinguishable 403 there would let a caller with just a stolen/leaked token (no private
// key needed) learn "this principal is quarantined," so it collapses into the same generic
// unauthenticated 401 every other bearer-validation failure produces.
// Concurrency note (104-028): this handler makes zero Update* calls — no sign-count/usage counter
// persisted on Credential, so nothing here writes back to the store at all.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.CompleteAgentTokenIssuance;

public sealed partial class CompleteAgentTokenIssuance
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentKeyChallengeStore ChallengeStore;
    private readonly IAgentTokenStore TokenStore;
    private readonly IOptions<AgentTokenOptions> Options;

    public Handler
    (
      IPrincipalStore principalStore,
      IAgentKeyChallengeStore challengeStore,
      IAgentTokenStore tokenStore,
      IOptions<AgentTokenOptions> options
    )
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      TokenStore = tokenStore;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      if (!WebAuthnPayloadDecoder.TryDecode(command.KeyId, out byte[] keyId)
        || !WebAuthnPayloadDecoder.TryDecode(command.Challenge, out byte[] challengeBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Signature, out byte[] signatureBytes))
      {
        return MalformedPayload();
      }

      if (!ChallengeStore.TryConsume(AgentKeyCeremonyType.TokenIssuance, challengeBytes))
      {
        return ChallengeInvalid();
      }

      var unknownScopes = command.Scopes.Where(scope => !AgentScopes.IsKnown(scope)).ToList();
      if (unknownScopes.Count > 0)
      {
        return InvalidScope(unknownScopes);
      }

      Credential? credential = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.AgentKey, keyId, cancellationToken);
      if (credential is null || credential.IsRevoked)
      {
        return IssuanceFailed();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(credential.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return IssuanceFailed();
      }

      AgentKeyProofResult verifyResult =
        AgentKeyProof.Verify(AgentKeyCeremonyType.TokenIssuance, credential.PublicMaterial, challengeBytes, signatureBytes);
      if (!verifyResult.IsValid)
      {
        return IssuanceFailed();
      }

      if (!principal.IsActive)
      {
        return Quarantined();
      }

      AgentTokenOptions agentTokenOptions = Options.Value;
      var lifetime = TimeSpan.FromMinutes(agentTokenOptions.TokenLifetimeMinutes);
      string accessToken = TokenStore.Issue(principal.Id, command.Scopes, lifetime);

      return new Response(accessToken, (int)lifetime.TotalSeconds, command.Scopes, principal.Id);
    }

    private static SharedProblemDetails MalformedPayload() => new()
    {
      Title = "Malformed request",
      Status = 400,
      Detail = "KeyId, Challenge, and Signature must be valid base64url."
    };

    private static SharedProblemDetails ChallengeInvalid() => new()
    {
      Title = "Challenge invalid",
      Status = 400,
      Detail = "The token issuance challenge is unknown, expired, or already used."
    };

    private static SharedProblemDetails InvalidScope(IReadOnlyCollection<string> unknownScopes) => new()
    {
      Title = "invalid_scope",
      Status = 400,
      Detail = $"Unknown scope(s): {string.Join(", ", unknownScopes)}."
    };

    private static SharedProblemDetails IssuanceFailed() => new()
    {
      Title = "Token issuance failed",
      Status = 400,
      Detail = "The agent key could not be verified."
    };

    private static SharedProblemDetails Quarantined() => new()
    {
      Title = "Account quarantined",
      Status = 403,
      Detail = "This account is currently restricted."
    };
  }
}
