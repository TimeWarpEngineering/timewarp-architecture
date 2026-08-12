#region Purpose
// Builds WebAuthn registration (navigator.credentials.create) options and verifies the browser's
// completed attestation response.
#endregion

#region Design
// CENTRAL DECISION (task 104-003): this is a hand-rolled minimal WebAuthn verifier built on BCL
// crypto + System.Formats.Cbor — NOT Fido2NetLib, NOT ASP.NET Core Identity's built-in passkey
// support.
//   - TimeWarp.Identity is a trust kernel published as a package; Fido2NetLib would become a
//     permanent public dependency of every consumer for attestation-format/MDS-trust-chain
//     machinery this template's posture never uses. System.Formats.Cbor is Microsoft/MIT and ships
//     in the ASP.NET Core shared framework too — BCL-adjacent, not a third-party dependency in the
//     same sense (same precedent as 104-028 treating System.Formats.Cbor-class BCL packages as
//     acceptable where a full third-party library would not be).
//   - ASP.NET Core Identity's passkey types are welded to UserManager/SignInManager, which inverts
//     104-002's Principal-owns-identity model — rejected for the same reason 104-027 rejected
//     StronglyTypedId: the adopted-library's main value is exactly the machinery this template does
//     not use.
//   - Scope is bounded by the template's own posture, not by what WebAuthn allows: attestation is
//     always requested "none", and attStmt is IGNORED regardless of what fmt an authenticator
//     actually returns (see attestation-object.cs) — registration verification is therefore
//     structural parsing + challenge/origin/rpIdHash binding + import-validating the COSE public
//     key, with NO attestation signature/trust-chain verification. ES256 (-7) and RS256 (-257) are
//     the only accepted algorithms; no MDS, no extensions, no EdDSA.
//   - Revisit trigger: if a deployment needs attestation policy or MDS trust-chain verification,
//     adopt Fido2NetLib at the HOST layer, fed the COSE public key this library already extracts and
//     stores — not inside TimeWarp.Identity. The verifier seam (WebAuthnRegistration/
//     WebAuthnAuthentication) is library-internal, so swapping it later moves no
//     contracts/handlers/endpoints.
//
// BuildOptionsJson emits PublicKeyCredentialCreationOptionsJSON shaped for the browser's
// PublicKeyCredential.parseCreationOptionsFromJSON: pubKeyCredParams offers both accepted
// algorithms so the authenticator (not this server) picks; residentKey/userVerification "preferred"
// favors discoverable credentials (sign-in without typing an identifier) without requiring a
// platform authenticator; attestation is always "none" (see above).
// hints: client-device then hybrid so create can offer phone/QR without authenticatorAttachment
// "platform" (which would suppress hybrid — task 165 / 147-005).
// Verify's check order is deliberate: parse clientDataJSON structurally first (type/challenge/
// origin), THEN parse attestationObject/authenticatorData, THEN rpIdHash/flags, THEN the outer
// credentialId parameter against authData's embedded credentialId, THEN import the COSE key. Every
// step past the null-argument guards is a Try*/comparison — non-null adversarial input (malformed
// JSON, garbage CBOR, wrong bytes) never throws; a caller (the complete-passkey-registration
// handler) that feeds this garbage always gets back a WebAuthnRegistrationResult, never an
// exception. Null arguments DO throw ArgumentNullException — that is a caller/handler bug, a
// different failure class than adversarial ceremony data.
#endregion

namespace TimeWarp.Identity;

public static class WebAuthnRegistration
{
  private const string CreateCeremonyType = "webauthn.create";
  private const int Es256 = -7;
  private const int Rs256 = -257;

  public static string BuildOptionsJson(WebAuthnRelyingParty rp, byte[] challenge, byte[] userHandle, string userName, string userDisplayName)
  {
    ArgumentNullException.ThrowIfNull(rp);

    var options = new
    {
      challenge = Base64Url.EncodeToString(challenge),
      rp = new { id = rp.Id, name = rp.Name },
      user = new
      {
        id = Base64Url.EncodeToString(userHandle),
        name = userName,
        displayName = userDisplayName
      },
      pubKeyCredParams = new object[]
      {
        new { type = "public-key", alg = Es256 },
        new { type = "public-key", alg = Rs256 }
      },
      authenticatorSelection = new { residentKey = "preferred", userVerification = "preferred" },
      // Soft preference: this device first, then hybrid (nearby phone / QR). Do NOT set
      // authenticatorAttachment — that would hard-exclude the other attachment class.
      hints = new[] { "client-device", "hybrid" },
      attestation = "none",
      timeout = 60_000
    };

    return JsonSerializer.Serialize(options);
  }

  public static WebAuthnRegistrationResult Verify(WebAuthnRelyingParty rp, byte[] expectedChallenge, byte[] clientDataJson, byte[] attestationObject, byte[] credentialId)
  {
    ArgumentNullException.ThrowIfNull(rp);
    ArgumentNullException.ThrowIfNull(expectedChallenge);
    ArgumentNullException.ThrowIfNull(clientDataJson);
    ArgumentNullException.ThrowIfNull(attestationObject);
    ArgumentNullException.ThrowIfNull(credentialId);

    if (!ClientData.TryParse(clientDataJson, out ClientData? clientData) || clientData is null)
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.MalformedClientData);

    if (!string.Equals(clientData.Type, CreateCeremonyType, StringComparison.Ordinal))
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.WrongCeremonyType);

    if (!Base64UrlHelpers.TryDecode(clientData.Challenge, out byte[] challengeBytes)
      || !challengeBytes.AsSpan().SequenceEqual(expectedChallenge))
    {
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.ChallengeMismatch);
    }

    if (!WebAuthnOrigin.IsAllowed(rp, clientData.Origin))
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.OriginMismatch);

    if (!AttestationObject.TryParse(attestationObject, out byte[]? authDataBytes) || authDataBytes is null)
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.MalformedAttestationObject);

    if (!AuthenticatorData.TryParse(authDataBytes, out AuthenticatorData authData))
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.MalformedAuthenticatorData);

    byte[] expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rp.Id));
    if (!authData.RpIdHash.AsSpan().SequenceEqual(expectedRpIdHash))
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.RpIdHashMismatch);

    if (!authData.UserPresent)
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.UserPresenceRequired);

    if (!authData.HasAttestedCredentialData || authData.CredentialId is null || authData.CosePublicKey is null)
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.MissingAttestedCredentialData);

    if (!authData.CredentialId.AsSpan().SequenceEqual(credentialId))
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.CredentialIdMismatch);

    if (!CoseKey.TryParse(new CborReader(authData.CosePublicKey), out CoseKey? coseKey) || coseKey is null)
      return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.MalformedCoseKey);

    // Explicit try/finally (not a using declaration): CA2000 cannot prove `algorithm` is null on
    // CoseKey.TryCreateVerifier's false path, so the canonical "local declared before try, disposed
    // unconditionally in finally" shape is what satisfies the analyzer here.
    AsymmetricAlgorithm? algorithm = null;
    try
    {
      if (!coseKey.TryCreateVerifier(out algorithm, out _))
      {
        return WebAuthnRegistrationResult.Failure(WebAuthnFailureReason.UnsupportedAlgorithm);
      }

      // Import-validated only (proves the key material is well-formed for its curve/modulus) — no
      // attestation signature is checked; see the CENTRAL DECISION Design region above.
      // AAGUID is returned so hosts can map to provider display names (Proton Pass, 1Password, …)
      // via PasskeyProviderNames — passkeys.io-style Settings labels (task 168).
      return WebAuthnRegistrationResult.Success(credentialId, authData.CosePublicKey, authData.Aaguid);
    }
    finally
    {
      algorithm?.Dispose();
    }
  }
}
