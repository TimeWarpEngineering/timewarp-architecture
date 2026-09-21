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

/// <summary>
/// Why <see cref="WebAuthnRegistration.Verify"/> or <see cref="WebAuthnAuthentication.Verify"/> rejected a ceremony.
/// </summary>
public enum WebAuthnFailureReason
{
  /// <summary>No failure; pairs with a valid registration or assertion result.</summary>
  None = 0,

  /// <summary>clientDataJSON could not be parsed as the expected WebAuthn shape.</summary>
  MalformedClientData,

  /// <summary>clientData type did not match the ceremony being verified (create vs get).</summary>
  WrongCeremonyType,

  /// <summary>Embedded challenge did not match the one-time challenge the store issued.</summary>
  ChallengeMismatch,

  /// <summary>Browser origin is outside the relying party's allow-list (or https/host fallback).</summary>
  OriginMismatch,

  /// <summary>attestationObject CBOR could not be parsed to authenticator data.</summary>
  MalformedAttestationObject,

  /// <summary>Authenticator data bytes are truncated or structurally invalid.</summary>
  MalformedAuthenticatorData,

  /// <summary>Authenticator data RP ID hash does not match SHA-256 of the configured RP ID.</summary>
  RpIdHashMismatch,

  /// <summary>User-present (UP) flag is clear; presence is required for both ceremonies.</summary>
  UserPresenceRequired,

  /// <summary>Registration authenticator data lacks attested credential id or COSE public key.</summary>
  MissingAttestedCredentialData,

  /// <summary>Outer credential id does not equal the id embedded in attested credential data.</summary>
  CredentialIdMismatch,

  /// <summary>COSE public key CBOR could not be parsed.</summary>
  MalformedCoseKey,

  /// <summary>COSE algorithm is outside the accepted set (ES256 / RS256).</summary>
  UnsupportedAlgorithm,

  /// <summary>Assertion signature does not verify over authenticatorData ‖ SHA-256(clientDataJSON).</summary>
  SignatureInvalid,
}
