#region Purpose
// Shared agent-key-registration ceremony preamble: decode → consume → TryParse → verify → handle-exists.
// Used by AddAgentKey and CompleteAgentKeyRegistration after any auth-guard step.
#endregion

#region Design
// Extracted from CompleteAgentKeyRegistration.Handler and AddAgentKey.Handler (task 131-002): both
// ladders matched after the optional auth guard, through the sequential FindCredentialByHandleAsync
// check. Callers keep: auth placement (Add only), Principal mint / attach, AddCredential try/catch,
// and response shaping (KeyId encoding).
//
// SECURITY-CRITICAL ORDER (one path, not N copies):
//   1. Decode PublicKey / Challenge / Signature (base64url) — fail MalformedPayload before any
//      store write.
//   2. TryConsume(Registration) BEFORE AgentKeyProof.Verify — even a payload that later fails
//      verification has already burned its challenge; retries must StartAgentKeyRegistration again.
//   3. AgentPublicKey.TryParse BEFORE Verify (task 104-004 §5): produces the server-computed KeyId
//      (needed for the duplicate-credential check and Credential.Create handle) as a distinct,
//      machine-readable "your public key is not usable" 400 — separate from a signature-mismatch
//      400 from Verify. No enumeration-oracle concern splitting these: this is a registration-
//      shaped ceremony; no credential lookup happens before either check, so nothing about an
//      existing account is disclosed by which of the two checks failed.
//   4. AgentKeyProof.Verify for AgentKeyCeremonyType.Registration.
//   5. FindCredentialByHandleAsync BEFORE Principal.Create / Credential.Create so sequential
//      duplicate-handle rejection never leaves an orphan Principal (Complete path). Concurrent
//      same-handle races still surface at AddCredentialAsync; callers catch and map to the same
//      CredentialAlreadyRegistered problem.
//
// Does NOT mint principals, issue tokens/sessions, or create credentials — those are handler-
// specific. Does NOT consume TokenIssuance challenges.
//
// Round-1 M5 (AddAgentKey): reusing AgentKeyCeremonyType.Registration for "add to existing principal"
// is intentional and safe — same intent-agnostic liveness reasoning as PasskeyRegistrationCeremony.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

internal static class AgentKeyRegistrationCeremony
{
  /// <summary>
  /// Verified registration materials ready for Credential.Create. keyId is the server-computed
  /// handle from AgentPublicKey.TryParse; publicKeyBytes is the decoded SPKI DER.
  /// </summary>
  internal sealed class Materials
  {
    public Materials(byte[] keyId, byte[] publicKeyBytes)
    {
      KeyId = keyId;
      PublicKeyBytes = publicKeyBytes;
    }

    public byte[] KeyId { get; }
    public byte[] PublicKeyBytes { get; }
  }

  public static async Task<OneOf<Materials, SharedProblemDetails>> TryCompleteAsync
  (
    string? publicKey,
    string? challenge,
    string? signature,
    IAgentKeyChallengeStore challengeStore,
    IPrincipalStore principalStore,
    CancellationToken cancellationToken
  )
  {
    if (!WebAuthnPayloadDecoder.TryDecode(publicKey, out byte[] publicKeyBytes)
      || !WebAuthnPayloadDecoder.TryDecode(challenge, out byte[] challengeBytes)
      || !WebAuthnPayloadDecoder.TryDecode(signature, out byte[] signatureBytes))
    {
      return IdentityProblems.MalformedPayload("PublicKey, Challenge, and Signature");
    }

    if (!challengeStore.TryConsume(AgentKeyCeremonyType.Registration, challengeBytes))
    {
      return IdentityProblems.ChallengeInvalid("registration");
    }

    if (!AgentPublicKey.TryParse(publicKeyBytes, out byte[] keyId))
    {
      return IdentityProblems.InvalidPublicKey();
    }

    AgentKeyProofResult verifyResult =
      AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, publicKeyBytes, challengeBytes, signatureBytes);
    if (!verifyResult.IsValid)
    {
      return IdentityProblems.AgentKeyRegistrationVerificationFailed(verifyResult.FailureReason);
    }

    Credential? existing =
      await principalStore.FindCredentialByHandleAsync(CredentialType.AgentKey, keyId, cancellationToken);
    if (existing is not null)
    {
      return IdentityProblems.CredentialAlreadyRegistered("agent key");
    }

    return new Materials(keyId, publicKeyBytes);
  }
}
