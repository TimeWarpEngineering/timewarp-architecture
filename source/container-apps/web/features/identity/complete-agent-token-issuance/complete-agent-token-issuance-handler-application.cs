#region Purpose
// Server-side handler for the CompleteAgentTokenIssuance command: verifies the agent's proof of
// possession for a previously-registered key and mints a scoped bearer access token on success.
#endregion

#region Design
// Order is deliberate and replay-safety-critical: decode -> consume the challenge -> validate scope
// shape (null/empty guard, then canonicalize, then check for unknown scopes) -> look up the
// credential/principal -> verify -> quarantine check -> issue. Consuming the challenge BEFORE
// verification means a tampered/replayed payload can never retry the same challenge, even when the
// request fails for an unrelated reason (e.g. an unknown scope) later in the same handler call.
// Single-consumer ladder — problems only extracted to IdentityProblems; no ceremony helper (task
// 131-002: token issuance not shared with a second handler).
// Null-Scopes defense-in-depth (round-1 finding M1): the contract Validator rejects a null/empty
// Scopes list before this handler ever runs; this handler's own null/empty check is belt-and-
// suspenders for a direct mediator Send that bypasses FluentValidationBehavior.
// Scope canonicalization (round-1 finding M4): duplicate entries are removed (StringComparer.Ordinal)
// immediately after the null guard, BEFORE the unknown-scope check, the store Issue call, and the
// Response — so a caller sending ["identity:read","identity:read"] gets a token/claims/response that
// all agree on ONE entry. The local `scopes` variable (not `command.Scopes`) is the canonical form
// used everywhere downstream.
// Scope validation (unknown scope -> 400 invalid_scope) runs BEFORE the credential lookup: this is a
// REQUEST-shape check — scope names are public constants (AgentScopes), so echoing unrecognized
// names discloses nothing about any principal/credential.
// No-enumeration-oracle posture: an unknown KeyId, a revoked credential, a missing principal, and a
// bad signature all return the SAME generic 400 "Token issuance failed."
// Quarantine is checked ONLY AFTER AgentKeyProof.Verify succeeds (403) — checking quarantine post-
// Verify makes "the caller has already cryptographically proven possession" true before the
// distinguishable 403 is reachable (mirrors CompletePasskeyAuthentication).
// At bearer VALIDATION time (not issuance), quarantine is a SILENT Fail -> 401 — see
// AgentTokenAuthenticationHandler's Design region. The two are deliberately different.
// Concurrency note (104-028): zero Update* calls.
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
        return IdentityProblems.MalformedPayload("KeyId, Challenge, and Signature");
      }

      if (!ChallengeStore.TryConsume(AgentKeyCeremonyType.TokenIssuance, challengeBytes))
      {
        return IdentityProblems.ChallengeInvalid("token issuance");
      }

      // Defense-in-depth against round-1 finding M1: the contract Validator now rejects a null/empty
      // Scopes list before this handler ever runs (FluentValidationBehavior), but this handler does
      // not trust that alone — a null here maps to the same invalid_scope 400 rather than an NRE on
      // .Where, in case that pipeline behavior is ever bypassed (e.g. a direct mediator Send).
      if (command.Scopes is null || command.Scopes.Count == 0)
      {
        return IdentityProblems.InvalidScope([]);
      }

      // Canonicalize before anything downstream sees the scope list (round-1 finding M4): duplicate
      // entries (e.g. ["identity:read","identity:read"]) would otherwise propagate uncanonicalized
      // into the stored grant, the claims the auth handler emits (one timewarp:scope claim per
      // entry — duplicates would emit duplicate claims), and both this Response and
      // GetAgentIdentity's echoed Scopes. Ordinal comparison matches AgentScopes.IsKnown's own
      // comparer.
      var scopes = command.Scopes.Distinct(StringComparer.Ordinal).ToList();

      var unknownScopes = scopes.Where(scope => !AgentScopes.IsKnown(scope)).ToList();
      if (unknownScopes.Count > 0)
      {
        return IdentityProblems.InvalidScope(unknownScopes);
      }

      Credential? credential = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.AgentKey, keyId, cancellationToken);
      if (credential is null || credential.IsRevoked)
      {
        return IdentityProblems.IssuanceFailed();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(credential.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return IdentityProblems.IssuanceFailed();
      }

      AgentKeyProofResult verifyResult =
        AgentKeyProof.Verify(AgentKeyCeremonyType.TokenIssuance, credential.PublicMaterial, challengeBytes, signatureBytes);
      if (!verifyResult.IsValid)
      {
        return IdentityProblems.IssuanceFailed();
      }

      if (!principal.IsActive)
      {
        return IdentityProblems.Quarantined();
      }

      AgentTokenOptions agentTokenOptions = Options.Value;
      var lifetime = TimeSpan.FromMinutes(agentTokenOptions.TokenLifetimeMinutes);
      string accessToken = TokenStore.Issue(principal.Id, scopes, lifetime);

      return new Response(accessToken, (int)lifetime.TotalSeconds, scopes, principal.Id);
    }
  }
}
