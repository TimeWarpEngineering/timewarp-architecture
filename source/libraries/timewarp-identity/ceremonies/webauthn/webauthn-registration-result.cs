#region Purpose
// Outcome of WebAuthnRegistration.Verify: either the verified credential id + public key, or why
// verification failed.
#endregion

#region Design
// Private constructor + internal factory methods (Success/Failure) instead of public settable
// properties — a caller can never construct a "valid" result with mismatched fields. CredentialId
// and CosePublicKey return defensive ToArray() copies (mirrors Credential's own byte[] copy-on-get,
// D8) so a caller cannot mutate the verifier's internal buffers.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Outcome of <see cref="WebAuthnRegistration.Verify"/> — verified credential material, or a typed failure reason.
/// </summary>
public sealed class WebAuthnRegistrationResult
{
  private readonly byte[] CredentialIdField;
  private readonly byte[] CosePublicKeyField;
  private readonly byte[]? AaguidField;

  private WebAuthnRegistrationResult(
    bool isValid,
    WebAuthnFailureReason failureReason,
    byte[] credentialId,
    byte[] cosePublicKey,
    byte[]? aaguid)
  {
    IsValid = isValid;
    FailureReason = failureReason;
    CredentialIdField = credentialId;
    CosePublicKeyField = cosePublicKey;
    AaguidField = aaguid;
  }

  /// <summary>True when registration structural checks and COSE key import succeeded.</summary>
  public bool IsValid { get; }

  /// <summary><see cref="WebAuthnFailureReason.None"/> on success; otherwise the reject cause.</summary>
  public WebAuthnFailureReason FailureReason { get; }

#pragma warning disable CA1819 // Binary material is intentionally exposed as byte[] copies
  /// <summary>Verified credential id bytes (defensive copy); empty on failure.</summary>
  public byte[] CredentialId => CredentialIdField.ToArray();

  /// <summary>Verified COSE public key bytes (defensive copy); empty on failure.</summary>
  public byte[] CosePublicKey => CosePublicKeyField.ToArray();

  /// <summary>16-byte authenticator AAGUID from attested credential data; empty when absent.</summary>
  public byte[] Aaguid => AaguidField is null ? [] : AaguidField.ToArray();
#pragma warning restore CA1819

  internal static WebAuthnRegistrationResult Success(byte[] credentialId, byte[] cosePublicKey, byte[]? aaguid) =>
    new(true, WebAuthnFailureReason.None, credentialId, cosePublicKey, aaguid?.ToArray());

  internal static WebAuthnRegistrationResult Failure(WebAuthnFailureReason reason) =>
    new(false, reason, [], [], aaguid: null);
}
