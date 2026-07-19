// ReSharper disable InconsistentNaming
namespace WebAuthnRegistration_;

public class Verify
{
  private static readonly WebAuthnRelyingParty Rp = new("localhost", "Test RP", []);
  private const string Origin = "https://localhost:7000";

  public void Happy_path_es256_succeeds()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.None);
    result.CredentialId.ShouldBe(authenticator.CredentialId);
    result.CosePublicKey.ShouldBe(authenticator.CosePublicKey);
  }

  public void Wrong_ceremony_type_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    // "webauthn.get" instead of "webauthn.create" — an authentication clientData replayed against
    // the registration endpoint.
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.WrongCeremonyType);
  }

  public void Wrong_challenge_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] signedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] expectedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", signedChallenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, expectedChallenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.ChallengeMismatch);
  }

  public void Wrong_origin_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, "https://evil.example");
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.OriginMismatch);
  }

  public void RpIdHash_mismatch_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    // authenticatorData built for a DIFFERENT rp id than Rp.Id — rpIdHash will not match.
    byte[] authenticatorData = authenticator.BuildAuthenticatorData("evil.example", includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.RpIdHashMismatch);
  }

  public void UserPresence_clear_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, userPresent: false, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UserPresenceRequired);
  }

  public void AttestedCredentialData_clear_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    // AT flag clear — no attested credential data, as if an assertion authenticatorData were
    // mistakenly submitted to registration.
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: false);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.MissingAttestedCredentialData);
  }

  public void CredentialId_mismatch_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    // The outer credentialId parameter (what a handler would decode from the contract's
    // CredentialId field) disagrees with the credentialId embedded in authData.
    byte[] differentCredentialId = [9, 9, 9, 9];

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, differentCredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.CredentialIdMismatch);
  }

  public void Unsupported_algorithm_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData
    (
      Rp.Id,
      includeAttestedCredentialData: true,
      cosePublicKeyOverride: SoftwareAuthenticator.BuildUnsupportedAlgorithmCoseKey()
    );
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UnsupportedAlgorithm);
  }

  public void Weak_rsa_modulus_fails()
  {
    // Round-1 finding M5: a structurally-valid RSA COSE key (kty=RSA, alg=RS256, parses fine) whose
    // modulus is only 512 bits must be rejected — CoseKey.TryCreateVerifier's MinimumRsaModulusBits
    // check, not RSA.ImportParameters (which would happily import it).
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData
    (
      Rp.Id,
      includeAttestedCredentialData: true,
      cosePublicKeyOverride: SoftwareAuthenticator.BuildWeakRsaCoseKey()
    );
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UnsupportedAlgorithm);
  }

  public void Malformed_cbor_attestation_object_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] garbageAttestationObject = [0xFF, 0xFF, 0xFF, 0x00, 0x01];

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, garbageAttestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.MalformedAttestationObject);
  }

  public void Fmt_packed_with_garbage_attStmt_is_accepted()
  {
    // Locks the template posture: attStmt is ignored regardless of what fmt an authenticator
    // returns — a "packed" attestation with nonsense attStmt content must still verify, because
    // this verifier never parses attStmt for any fmt.
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData, fmt: "packed", garbageAttStmt: true);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeTrue();
  }
}
