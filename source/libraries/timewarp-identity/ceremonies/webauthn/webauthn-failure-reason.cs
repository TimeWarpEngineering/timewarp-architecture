#region Purpose
// Enumerates every structural/cryptographic reason WebAuthnRegistration.Verify or
// WebAuthnAuthentication.Verify can reject a ceremony, so callers can log or branch on cause
// without parsing exception messages.
#endregion

#region Design
// Reserved zero (None) pairs with WebAuthnRegistrationResult/WebAuthnAssertionResult.IsValid true —
// a valid result always carries FailureReason.None, so the two fields can never disagree.
// Shared by both ceremonies rather than split into two enums: registration and authentication share
// most failure modes (malformed client data, wrong ceremony type, challenge/origin/rpIdHash
// mismatch, unsupported algorithm); the few ceremony-specific ones (MissingAttestedCredentialData,
// CredentialIdMismatch for registration; SignatureInvalid for authentication) simply never occur on
// the other path.
#endregion

namespace TimeWarp.Identity;

public enum WebAuthnFailureReason
{
  None = 0,
  MalformedClientData,
  WrongCeremonyType,
  ChallengeMismatch,
  OriginMismatch,
  MalformedAttestationObject,
  MalformedAuthenticatorData,
  RpIdHashMismatch,
  UserPresenceRequired,
  MissingAttestedCredentialData,
  CredentialIdMismatch,
  MalformedCoseKey,
  UnsupportedAlgorithm,
  SignatureInvalid,
}
