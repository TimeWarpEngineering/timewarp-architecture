// ReSharper disable InconsistentNaming
namespace WebAuthnRegistration_;

public class Verify
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Verify>();

  private static readonly WebAuthnRelyingParty Rp = new("localhost", "Test RP", []);
  private const string Origin = "https://localhost:7000";

  public static Task Happy_path_es256_succeeds()
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
    return Task.CompletedTask;
  }

  public static Task Wrong_ceremony_type_fails()
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
    return Task.CompletedTask;
  }

  public static Task Wrong_challenge_fails()
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
    return Task.CompletedTask;
  }

  public static Task Wrong_origin_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, "https://evil.example");
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.OriginMismatch);
    return Task.CompletedTask;
  }

  public static Task RpIdHash_mismatch_fails()
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
    return Task.CompletedTask;
  }

  public static Task UserPresence_clear_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, userPresent: false, includeAttestedCredentialData: true);
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UserPresenceRequired);
    return Task.CompletedTask;
  }

  public static Task AttestedCredentialData_clear_fails()
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
    return Task.CompletedTask;
  }

  public static Task CredentialId_mismatch_fails()
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
    return Task.CompletedTask;
  }

  public static Task Unsupported_algorithm_fails()
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
    return Task.CompletedTask;
  }

  public static Task Weak_rsa_modulus_fails()
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
    return Task.CompletedTask;
  }

  public static Task Empty_rsa_modulus_fails_without_throwing()
  {
    // Round-2 finding M9: CoseKey.TryParse only null-checks the RSA modulus, so a zero-length `n`
    // reaches TryCreateVerifier. Before the fix, GetModulusBitLength indexed modulus[0] on an empty
    // array and threw IndexOutOfRangeException, uncaught by TryCreateVerifier's
    // catch (CryptographicException) — an unhandled 500 on adversarial input. Must reject cleanly.
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData
    (
      Rp.Id,
      includeAttestedCredentialData: true,
      cosePublicKeyOverride: SoftwareAuthenticator.BuildEmptyModulusRsaCoseKey()
    );
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UnsupportedAlgorithm);
    return Task.CompletedTask;
  }

  public static Task Empty_rsa_exponent_fails_without_throwing()
  {
    // Sibling gap found auditing M9's neighborhood: a real, large-enough modulus but a zero-length
    // exponent. On this platform RSA.ImportParameters throws IndexOutOfRangeException (not
    // CryptographicException) for an empty Exponent — the same uncaught-exception class as M9,
    // independently reachable. Must reject cleanly, never reach ImportParameters.
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData
    (
      Rp.Id,
      includeAttestedCredentialData: true,
      cosePublicKeyOverride: SoftwareAuthenticator.BuildEmptyExponentRsaCoseKey()
    );
    byte[] attestationObject = SoftwareAuthenticator.BuildAttestationObject(authenticatorData);

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, attestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UnsupportedAlgorithm);
    return Task.CompletedTask;
  }

  public static Task Malformed_cbor_attestation_object_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, Origin);
    byte[] garbageAttestationObject = [0xFF, 0xFF, 0xFF, 0x00, 0x01];

    WebAuthnRegistrationResult result = WebAuthnRegistration.Verify(Rp, challenge, clientDataJson, garbageAttestationObject, authenticator.CredentialId);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.MalformedAttestationObject);
    return Task.CompletedTask;
  }

  public static Task Fmt_packed_with_garbage_attStmt_is_accepted()
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
    return Task.CompletedTask;
  }
}
