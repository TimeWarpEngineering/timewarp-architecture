#region Purpose
// Binary parse of the WebAuthn authenticatorData structure (spec §6.1) shared by both attestation
// (inside attestationObject) and assertion (sent directly) ceremonies.
#endregion

#region Design
// Layout: rpIdHash(32) | flags(1) | signCount(4) | [attestedCredentialData if AT flag set: aaguid(16)
// | credentialIdLength(2, big-endian) | credentialId | credentialPublicKey(CBOR, remaining bytes)].
// signCount is parsed but deliberately not exposed as an enforced invariant: synced/cloud passkeys
// commonly report 0 on every use (no hardware counter to increment), so a strict "must be
// monotonically increasing" check would reject legitimate synced-passkey authentications. Credential
// has no sign-count field to persist against; revisit if/when 104-005/104-006 add cloned-credential
// detection.
// The trailing "remaining bytes" handed back as CosePublicKey is NOT validated to be exactly one
// CBOR value — if the ED (extension data) flag were set, extension bytes would trail the COSE key
// in that same slice. This template posture never requests extensions, so no extension parsing is
// implemented; CoseKey.TryParse simply stops consuming once it has read one complete CBOR map, and
// any trailing bytes (which should not occur under this posture) are silently unexamined rather
// than causing a parse failure — matches the same "ignore what we don't use" posture as attStmt.
#endregion

namespace TimeWarp.Identity;

internal readonly struct AuthenticatorData
{
  private const int RpIdHashLength = 32;
  private const int FlagsLength = 1;
  private const int SignCountLength = 4;
  private const int MinimumLength = RpIdHashLength + FlagsLength + SignCountLength;
  private const int AaguidLength = 16;
  private const int CredentialIdLengthFieldSize = 2;

  private const byte UserPresentFlag = 0x01;
  private const byte UserVerifiedFlag = 0x04;
  private const byte AttestedCredentialDataFlag = 0x40;

  public required byte[] RpIdHash { get; init; }
  public required bool UserPresent { get; init; }
  public required bool UserVerified { get; init; }
  public required bool HasAttestedCredentialData { get; init; }
  public required uint SignCount { get; init; }
  public byte[]? Aaguid { get; init; }
  public byte[]? CredentialId { get; init; }
  public byte[]? CosePublicKey { get; init; }

  public static bool TryParse(byte[] data, out AuthenticatorData result)
  {
    result = default;
    if (data.Length < MinimumLength) return false;

    byte[] rpIdHash = data[..RpIdHashLength];
    byte flags = data[RpIdHashLength];
    uint signCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(RpIdHashLength + FlagsLength, SignCountLength));

    bool userPresent = (flags & UserPresentFlag) != 0;
    bool userVerified = (flags & UserVerifiedFlag) != 0;
    bool hasAttestedCredentialData = (flags & AttestedCredentialDataFlag) != 0;

    if (!hasAttestedCredentialData)
    {
      result = new AuthenticatorData
      {
        RpIdHash = rpIdHash,
        UserPresent = userPresent,
        UserVerified = userVerified,
        HasAttestedCredentialData = false,
        SignCount = signCount
      };
      return true;
    }

    int offset = MinimumLength;
    if (data.Length < offset + AaguidLength + CredentialIdLengthFieldSize) return false;

    byte[] aaguid = data[offset..(offset + AaguidLength)];
    offset += AaguidLength;

    ushort credentialIdLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, CredentialIdLengthFieldSize));
    offset += CredentialIdLengthFieldSize;

    if (data.Length < offset + credentialIdLength) return false;

    byte[] credentialId = data[offset..(offset + credentialIdLength)];
    offset += credentialIdLength;

    if (offset >= data.Length) return false; // must have at least some bytes for the COSE public key

    byte[] cosePublicKey = data[offset..];

    result = new AuthenticatorData
    {
      RpIdHash = rpIdHash,
      UserPresent = userPresent,
      UserVerified = userVerified,
      HasAttestedCredentialData = true,
      SignCount = signCount,
      Aaguid = aaguid,
      CredentialId = credentialId,
      CosePublicKey = cosePublicKey
    };

    return true;
  }
}
