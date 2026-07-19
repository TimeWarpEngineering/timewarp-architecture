#region Purpose
// Builds WebAuthn authentication (navigator.credentials.get) options and verifies the browser's
// completed assertion response.
#endregion

#region Design
// allowCredentials is always empty — discoverable-credential-first per the task's requirement
// ("prefer discoverable credentials — sign-in without typing email"); the platform's passkey UI
// lets the user pick from any resident credential scoped to this RP ID rather than the server
// pre-listing candidate credential ids (which would require knowing the user's identity before
// authentication starts — exactly what a discoverable flow avoids).
// Verify is the only place in this library that performs an actual cryptographic signature check:
// ECDsa.VerifyData(authenticatorData ‖ SHA-256(clientDataJSON), signature, SHA256,
// DSASignatureFormat.Rfc3279DerSequence) for ES256 (WebAuthn ECDSA signatures are DER-encoded), or
// RSA.VerifyData(..., RSASignaturePadding.Pkcs1) for RS256. The signed-data construction
// (authData‖clientDataHash) is fixed by the WebAuthn spec (§6.3.3) — the client-data hash, not the
// raw clientDataJSON bytes, is what gets concatenated.
// UV (user-verified) is read into AuthenticatorData but NOT enforced here — userVerification
// "preferred" in BuildOptionsJson means a passkey without local verification (e.g. a password
// manager that only proves possession) must still succeed; only UP (user-present) is required.
// signCount is parsed but ignored (see authenticator-data.cs) — 0-and-regressing both pass by
// design for synced passkeys.
#endregion

namespace TimeWarp.Identity;

public static class WebAuthnAuthentication
{
  private const string GetCeremonyType = "webauthn.get";

  public static string BuildOptionsJson(WebAuthnRelyingParty rp, byte[] challenge)
  {
    ArgumentNullException.ThrowIfNull(rp);

    var options = new
    {
      challenge = Base64Url.EncodeToString(challenge),
      rpId = rp.Id,
      allowCredentials = Array.Empty<object>(),
      userVerification = "preferred",
      timeout = 60_000
    };

    return JsonSerializer.Serialize(options);
  }

  public static WebAuthnAssertionResult Verify(WebAuthnRelyingParty rp, byte[] expectedChallenge, byte[] storedCosePublicKey, byte[] clientDataJson, byte[] authenticatorData, byte[] signature)
  {
    ArgumentNullException.ThrowIfNull(rp);
    ArgumentNullException.ThrowIfNull(expectedChallenge);
    ArgumentNullException.ThrowIfNull(storedCosePublicKey);
    ArgumentNullException.ThrowIfNull(clientDataJson);
    ArgumentNullException.ThrowIfNull(authenticatorData);
    ArgumentNullException.ThrowIfNull(signature);

    if (!ClientData.TryParse(clientDataJson, out ClientData? clientData) || clientData is null)
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.MalformedClientData);

    if (!string.Equals(clientData.Type, GetCeremonyType, StringComparison.Ordinal))
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.WrongCeremonyType);

    if (!Base64UrlHelpers.TryDecode(clientData.Challenge, out byte[] challengeBytes)
      || !challengeBytes.AsSpan().SequenceEqual(expectedChallenge))
    {
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.ChallengeMismatch);
    }

    if (!WebAuthnOrigin.IsAllowed(rp, clientData.Origin))
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.OriginMismatch);

    if (!AuthenticatorData.TryParse(authenticatorData, out AuthenticatorData authData))
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.MalformedAuthenticatorData);

    byte[] expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rp.Id));
    if (!authData.RpIdHash.AsSpan().SequenceEqual(expectedRpIdHash))
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.RpIdHashMismatch);

    if (!authData.UserPresent)
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.UserPresenceRequired);

    if (!CoseKey.TryParse(new CborReader(storedCosePublicKey), out CoseKey? coseKey) || coseKey is null)
      return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.MalformedCoseKey);

    // Explicit try/finally (not a using declaration): CA2000 cannot prove `algorithm` is null on
    // CoseKey.TryCreateVerifier's false path, so the canonical "local declared before try, disposed
    // unconditionally in finally" shape is what satisfies the analyzer here.
    AsymmetricAlgorithm? algorithm = null;
    try
    {
      if (!coseKey.TryCreateVerifier(out algorithm, out HashAlgorithmName hashAlgorithm))
      {
        return WebAuthnAssertionResult.Failure(WebAuthnFailureReason.UnsupportedAlgorithm);
      }

      byte[] clientDataHash = SHA256.HashData(clientDataJson);
      byte[] signedData = new byte[authenticatorData.Length + clientDataHash.Length];
      authenticatorData.CopyTo(signedData, 0);
      clientDataHash.CopyTo(signedData, authenticatorData.Length);

      bool verified = algorithm switch
      {
        ECDsa ecdsa => ecdsa.VerifyData(signedData, signature, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence),
        RSA rsa => rsa.VerifyData(signedData, signature, hashAlgorithm, RSASignaturePadding.Pkcs1),
        _ => false
      };

      return verified
        ? WebAuthnAssertionResult.Success()
        : WebAuthnAssertionResult.Failure(WebAuthnFailureReason.SignatureInvalid);
    }
    finally
    {
      algorithm?.Dispose();
    }
  }
}
