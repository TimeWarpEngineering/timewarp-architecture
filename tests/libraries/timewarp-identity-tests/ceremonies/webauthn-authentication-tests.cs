// ReSharper disable InconsistentNaming
namespace WebAuthnAuthentication_;

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
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.None);
    return Task.CompletedTask;
  }

  public static Task Happy_path_rs256_succeeds()
  {
    var authenticator = new SoftwareAuthenticator(useRsa: true);
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.None);
    return Task.CompletedTask;
  }

  public static Task Tampered_signature_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);
    signature[0] ^= 0xFF; // flip a bit — no longer verifies against the signed data

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.SignatureInvalid);
    return Task.CompletedTask;
  }

  public static Task Tampered_authenticatorData_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] signedAuthenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, signCount: 1);
    byte[] signature = authenticator.Sign(signedAuthenticatorData, clientDataJson);

    // A DIFFERENT authenticatorData (different signCount) is presented alongside the signature that
    // was computed over the original — the signed-data bytes no longer match what was signed.
    byte[] tamperedAuthenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, signCount: 2);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, tamperedAuthenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.SignatureInvalid);
    return Task.CompletedTask;
  }

  public static Task Wrong_challenge_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] signedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] expectedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", signedChallenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, expectedChallenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.ChallengeMismatch);
    return Task.CompletedTask;
  }

  public static Task Wrong_origin_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, "https://evil.example");
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.OriginMismatch);
    return Task.CompletedTask;
  }

  public static Task RpIdHash_mismatch_fails()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData("evil.example");
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.RpIdHashMismatch);
    return Task.CompletedTask;
  }

  public static Task UserPresence_clear_is_rejected()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, userPresent: false);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(WebAuthnFailureReason.UserPresenceRequired);
    return Task.CompletedTask;
  }

  public static Task UserVerification_clear_is_accepted()
  {
    // userVerification is "preferred", not required — a passkey/security key that only proves
    // possession (UV clear) must still succeed.
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, userVerified: false);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task SignCount_zero_passes()
  {
    var authenticator = new SoftwareAuthenticator();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, Origin);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, signCount: 0);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    WebAuthnAssertionResult result = WebAuthnAuthentication.Verify(Rp, challenge, authenticator.CosePublicKey, clientDataJson, authenticatorData, signature);

    result.IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task SignCount_regressing_still_passes()
  {
    // No sign-count persistence exists on Credential (see authenticator-data.cs) — a "regressing"
    // count (as a cloned/synced authenticator would report) must still verify.
    var authenticator = new SoftwareAuthenticator();

    byte[] firstChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] firstClientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", firstChallenge, Origin);
    byte[] firstAuthenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, signCount: 100);
    byte[] firstSignature = authenticator.Sign(firstAuthenticatorData, firstClientDataJson);
    WebAuthnAuthentication.Verify(Rp, firstChallenge, authenticator.CosePublicKey, firstClientDataJson, firstAuthenticatorData, firstSignature)
      .IsValid.ShouldBeTrue();

    byte[] secondChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] secondClientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.get", secondChallenge, Origin);
    byte[] secondAuthenticatorData = authenticator.BuildAuthenticatorData(Rp.Id, signCount: 5);
    byte[] secondSignature = authenticator.Sign(secondAuthenticatorData, secondClientDataJson);

    WebAuthnAssertionResult result =
      WebAuthnAuthentication.Verify(Rp, secondChallenge, authenticator.CosePublicKey, secondClientDataJson, secondAuthenticatorData, secondSignature);

    result.IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }
}
