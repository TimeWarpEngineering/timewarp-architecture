#region Purpose
// Server-side handler for the CompleteAgentKeyRegistration command: verifies the agent's proof of
// possession and mints a Principal + Credential on success.
#endregion

#region Design
// Order is deliberate and replay-safety-critical, mirroring CompletePasskeyRegistration.Handler:
// decode -> consume the challenge -> verify -> check for an existing credential -> mint. The
// challenge is consumed (one-time) BEFORE AgentKeyProof.Verify runs — even a payload that later
// fails verification has already burned its challenge, so a retry must start a brand-new ceremony
// (StartAgentKeyRegistration).
// AgentPublicKey.TryParse runs BEFORE AgentKeyProof.Verify (task 104-004 §5): TryParse produces the
// server-computed KeyId (needed for the duplicate-credential check and the eventual Credential.Create
// handle) as a distinct, machine-readable "your public key is not usable" 400 — separate from a
// "the signature does not match" 400 from Verify. There is no enumeration-oracle concern in
// splitting these two checks here (unlike CompleteAgentTokenIssuance): this is a REGISTRATION
// ceremony, no credential lookup happens before either check, so nothing about an existing account
// is ever disclosed by which of the two checks failed.
// FindCredentialByHandleAsync runs BEFORE Principal.Create/AddPrincipalAsync so a duplicate-handle
// rejection never leaves an orphan Principal with no credential in the sequential (common) case.
// AddCredentialAsync is still wrapped in try/catch(InvalidOperationException) for the residual
// concurrent-race window (two ceremonies registering the SAME key concurrently) — mirrors
// CompletePasskeyRegistration.Handler exactly (104-003 round-1 finding M3): the store enforces
// handle uniqueness atomically, so the race can surface as this catch translating to the same 409
// instead of an unhandled 500; the orphan Principal from the losing race is NOT compensated —
// IPrincipalStore has no delete method yet (see that Design region for the full rationale, not
// re-derived here).
// No sponsor, no cookie: agents need no linked human (task requirement) and are never issued a
// browser session — only a scoped bearer token, minted separately by CompleteAgentTokenIssuance.
// Concurrency note (104-028): this handler makes zero Update* calls. AddPrincipalAsync/
// AddCredentialAsync are both Add* (no version check); AddCredentialAsync's first-credential rule
// auto-promotes the STORED principal Provisional -> Keyed internally, kind-agnostically (Agent
// principals promote exactly like Human ones) — this handler's in-hand `principal` local is
// deliberately left stale afterward, same as CompletePasskeyRegistration.Handler.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.CompleteAgentKeyRegistration;

public sealed partial class CompleteAgentKeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentKeyChallengeStore ChallengeStore;

    public Handler(IPrincipalStore principalStore, IAgentKeyChallengeStore challengeStore)
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      if (!WebAuthnPayloadDecoder.TryDecode(command.PublicKey, out byte[] publicKeyBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Challenge, out byte[] challengeBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Signature, out byte[] signatureBytes))
      {
        return MalformedPayload();
      }

      if (!ChallengeStore.TryConsume(AgentKeyCeremonyType.Registration, challengeBytes))
      {
        return ChallengeInvalid();
      }

      if (!AgentPublicKey.TryParse(publicKeyBytes, out byte[] keyId))
      {
        return InvalidPublicKey();
      }

      AgentKeyProofResult verifyResult = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, publicKeyBytes, challengeBytes, signatureBytes);
      if (!verifyResult.IsValid)
      {
        return VerificationFailed(verifyResult.FailureReason);
      }

      Credential? existing = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.AgentKey, keyId, cancellationToken);
      if (existing is not null)
      {
        return CredentialAlreadyRegistered();
      }

      var principal = Principal.Create(PrincipalKind.Agent);
      await PrincipalStore.AddPrincipalAsync(principal, cancellationToken);

      var credential = Credential.Create(principal.Id, CredentialType.AgentKey, keyId, publicKeyBytes, command.Label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        return CredentialAlreadyRegistered();
      }

      return new Response(principal.Id, Base64Url.EncodeToString(keyId));
    }

    private static SharedProblemDetails MalformedPayload() => new()
    {
      Title = "Malformed request",
      Status = 400,
      Detail = "PublicKey, Challenge, and Signature must be valid base64url."
    };

    private static SharedProblemDetails ChallengeInvalid() => new()
    {
      Title = "Challenge invalid",
      Status = 400,
      Detail = "The registration challenge is unknown, expired, or already used."
    };

    private static SharedProblemDetails InvalidPublicKey() => new()
    {
      Title = "Invalid public key",
      Status = 400,
      Detail = "PublicKey must be a well-formed ECDSA P-256 SubjectPublicKeyInfo (DER)."
    };

    private static SharedProblemDetails VerificationFailed(AgentKeyFailureReason reason) => new()
    {
      Title = "Agent key registration verification failed",
      Status = 400,
      Detail = $"Verification failed: {reason}."
    };

    private static SharedProblemDetails CredentialAlreadyRegistered() => new()
    {
      Title = "Credential already registered",
      Status = 409,
      Detail = "This agent key is already registered to an account."
    };
  }
}
