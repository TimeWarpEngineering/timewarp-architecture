#region Purpose
// Server-side handler for the AddAgentKey command: verifies the agent's proof of possession and
// attaches a new Credential to the CALLER's EXISTING principal (never mints a new one) — also how
// agent key ROTATION works, paired with a subsequent RevokeCredential of the old key.
#endregion

#region Design
// Order mirrors CompleteAgentKeyRegistration.Handler exactly (decode -> consume the challenge ->
// TryParse the public key -> verify -> check for an existing credential -> attach), with the same two
// differences as AddPasskey.Handler vs CompletePasskeyRegistration.Handler: the principal id comes
// from ICurrentPrincipalAccessor (resolved first, before touching ceremony state), and there is no
// session/token issuance side effect — adding a key does not itself mint a bearer token; the caller
// requests one separately via CompleteAgentTokenIssuance using the new KeyId this response returns.
// No orphan-Principal residual (same reasoning as AddPasskey.Handler): this handler never calls
// AddPrincipalAsync, so a lost same-handle race has nothing left over to compensate.
// AgentPublicKey.TryParse still runs BEFORE AgentKeyProof.Verify (task 104-004 §5 precedent) — and
// there remains no enumeration-oracle concern in splitting the two checks here, for the same reason
// as CompleteAgentKeyRegistration.Handler's Design region: this is a registration-shaped ceremony (no
// credential lookup happens before either check), so nothing about an existing account is disclosed
// by which check failed.
// No PrincipalKind vs CredentialType affinity check (Wave-1, deliberate, not an oversight): nothing
// in the domain model (Principal.cs, Credential.cs) ties a principal's Kind to which CredentialType it
// may hold — a Human principal COULD end up with an AgentKey credential attached via this endpoint
// (or an Agent principal with a Passkey via AddPasskey.Handler, though a Passkey ceremony practically
// requires a browser an agent process would not have). Adding such a restriction was not requested by
// the task and is not implied by any existing invariant; if a real product need for kind/credential
// affinity emerges, it belongs on the domain (Principal/Credential), not bolted onto this one handler.
// Zero Update* calls (Add* only) — no concurrency retry loop needed here, unlike RevokeCredential.
// Round-1 review (M5, security-confirmed no risk): this handler consumes the SAME
// AgentKeyCeremonyType.Registration challenge type StartAgentKeyRegistration/
// CompleteAgentKeyRegistration use, with no separate "add" ceremony type — same reasoning as
// AddPasskey.Handler's Design region. The challenge is an intent-agnostic one-time liveness proof
// (proves possession of the private key for this public key, nothing about WHOSE principal the
// resulting credential should attach to); the new-principal-vs-add-to-current-principal distinction
// is enforced entirely by this endpoint's own [EndpointAuthorize(Policy="credential-management")]
// boundary and by sourcing the target principal id from ICurrentPrincipalAccessor, never by which
// challenge type was consumed — so reusing the Registration ceremony type introduces no
// confused-deputy risk.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.AddAgentKey;

public sealed partial class AddAgentKey
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentKeyChallengeStore ChallengeStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    public Handler
    (
      IPrincipalStore principalStore,
      IAgentKeyChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor
    )
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return Unauthenticated();
      }

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

      var credential = Credential.Create(callerId.Value, CredentialType.AgentKey, keyId, publicKeyBytes, command.Label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        return CredentialAlreadyRegistered();
      }

      return new Response(credential.Id, Base64Url.EncodeToString(keyId));
    }

    private static SharedProblemDetails Unauthenticated() => new()
    {
      Title = "Unauthenticated",
      Status = 401,
      Detail = "No authenticated principal."
    };

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
