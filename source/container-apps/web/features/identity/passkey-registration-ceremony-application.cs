#region Purpose
// Shared passkey-registration ceremony preamble: decode → consume → verify → handle-exists.
// Used by AddPasskey and CompletePasskeyRegistration after their auth-guard / RP selection steps.
#endregion

#region Design
// Extracted from CompletePasskeyRegistration.Handler and AddPasskey.Handler (task 131-002): both
// ladders matched byte-for-byte after caller auth-guard + RP select, through the sequential
// FindCredentialByHandleAsync check. Callers keep: auth placement, RP selection, Principal mint /
// attach, AddCredential try/catch, session issue, and residual orphan notes.
//
// SECURITY-CRITICAL ORDER (one path, not N copies):
//   1. Decode CredentialId / ClientDataJson / AttestationObject (base64url) — fail MalformedPayload
//      before any store write.
//   2. Read challenge from clientDataJSON and TryConsume(Registration) BEFORE WebAuthnRegistration
//      .Verify — even a payload that later fails verification has already burned its challenge, so
//      a retry with a corrected payload must start a brand-new ceremony (StartPasskeyRegistration)
//      rather than resubmitting a tampered version of an already-answered one. Challenge consume
//      is one-time and intentional; RP select must already have run in the caller so a disallowed
//      host never reaches this method (and never burns a challenge).
//   3. WebAuthnRegistration.Verify binds rpIdHash/origin against the caller's selected relying party.
//   4. FindCredentialByHandleAsync BEFORE any Principal.Create / Credential.Create so the common
//      sequential duplicate-handle case 409s without minting an orphan Principal (Complete path)
//      or attaching a colliding credential (Add path). Concurrent same-handle races that pass this
//      check still surface at AddCredentialAsync; callers catch InvalidOperationException and map
//      to the same CredentialAlreadyRegistered problem.
//
// Does NOT consume challenges for Authentication — Registration only. Does NOT Issue sessions or
// create principals; those are handler-specific post-verify actions.
//
// Round-1 M5 (AddPasskey): reusing WebAuthnCeremonyType.Registration for "add to existing principal"
// is intentional and safe — the challenge is an intent-agnostic liveness proof; principal targeting
// is enforced by the caller's auth boundary + ICurrentPrincipalAccessor, never by challenge type.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

internal static class PasskeyRegistrationCeremony
{
  /// <summary>
  /// Verified registration materials ready for Credential.Create. Defensive copies of library
  /// byte[] surfaces so callers own the buffers after the ceremony returns.
  /// </summary>
  internal sealed class Materials
  {
    public Materials(byte[] credentialId, byte[] cosePublicKey)
    {
      CredentialId = credentialId;
      CosePublicKey = cosePublicKey;
    }

    public byte[] CredentialId { get; }
    public byte[] CosePublicKey { get; }
  }

  public static async Task<OneOf<Materials, SharedProblemDetails>> TryCompleteAsync
  (
    string? credentialId,
    string? clientDataJson,
    string? attestationObject,
    WebAuthnRelyingParty relyingParty,
    IWebAuthnChallengeStore challengeStore,
    IPrincipalStore principalStore,
    CancellationToken cancellationToken
  )
  {
    if (!WebAuthnPayloadDecoder.TryDecode(credentialId, out byte[] credentialIdBytes)
      || !WebAuthnPayloadDecoder.TryDecode(clientDataJson, out byte[] clientDataJsonBytes)
      || !WebAuthnPayloadDecoder.TryDecode(attestationObject, out byte[] attestationObjectBytes))
    {
      return IdentityProblems.MalformedPayload("CredentialId, ClientDataJson, and AttestationObject");
    }

    if (!WebAuthnChallengeReader.TryReadChallenge(clientDataJsonBytes, out byte[] challenge)
      || !challengeStore.TryConsume(WebAuthnCeremonyType.Registration, challenge))
    {
      return IdentityProblems.ChallengeInvalid("registration");
    }

    WebAuthnRegistrationResult verifyResult =
      WebAuthnRegistration.Verify(relyingParty, challenge, clientDataJsonBytes, attestationObjectBytes, credentialIdBytes);

    if (!verifyResult.IsValid)
    {
      return IdentityProblems.PasskeyRegistrationVerificationFailed(verifyResult.FailureReason);
    }

    Credential? existing =
      await principalStore.FindCredentialByHandleAsync(CredentialType.Passkey, verifyResult.CredentialId, cancellationToken);
    if (existing is not null)
    {
      return IdentityProblems.CredentialAlreadyRegistered("passkey");
    }

    return new Materials(verifyResult.CredentialId, verifyResult.CosePublicKey);
  }
}
