#region Purpose
// Deterministic software WebAuthn authenticator: builds spec-correct clientDataJSON/
// authenticatorData/attestationObject/COSE keys and signs assertions, using FIXED test-only
// keypairs (never randomly generated per run) so a verifier failure reproduces identically on every
// test execution.
#endregion

#region Design
// The ES256 (P-256) and RS256 (RSA-2048) keypairs below were generated once (offline) and are
// embedded as literal byte arrays — there is no RNG anywhere in this fixture (CredentialId is a
// fixed constant too), matching the "no Date.now-style nondeterminism in fixtures" requirement.
// This is the keystone test fixture for webauthn-registration-tests.cs and
// webauthn-authentication-tests.cs: every happy-path and negative-path vector in both files is
// built by taking this fixture's spec-correct output and deliberately corrupting exactly one field,
// so a test failure isolates to the one thing WebAuthnRegistration/WebAuthnAuthentication.Verify
// got wrong.
#endregion

namespace TimeWarp.Identity.Tests.Ceremonies.Infrastructure;

internal sealed class SoftwareAuthenticator
{
  // Fixed ES256 (P-256) test keypair.
  private static readonly byte[] Es256D =
  [
    57, 98, 112, 3, 49, 30, 186, 227, 241, 223, 145, 248, 211, 242, 39, 195, 59, 75, 114, 255, 248,
    40, 208, 247, 17, 14, 24, 181, 75, 168, 123, 177
  ];

  private static readonly byte[] Es256X =
  [
    247, 42, 163, 84, 60, 146, 118, 254, 252, 140, 85, 133, 242, 73, 68, 153, 116, 128, 35, 84, 86,
    253, 22, 210, 225, 44, 241, 112, 231, 76, 131, 232
  ];

  private static readonly byte[] Es256Y =
  [
    203, 110, 94, 127, 2, 93, 92, 236, 29, 9, 180, 183, 207, 238, 7, 132, 113, 165, 97, 16, 209, 110,
    69, 253, 96, 188, 107, 95, 64, 43, 163, 13
  ];

  // Fixed RS256 (RSA-2048) test keypair — supports the plan's "one RS256 path" cases.
  private static readonly byte[] Rs256Modulus =
  [
    250, 165, 20, 227, 188, 107, 240, 169, 145, 63, 198, 174, 253, 194, 16, 141, 79, 252, 45, 47, 48,
    99, 247, 45, 165, 180, 71, 158, 177, 252, 39, 220, 73, 201, 121, 238, 51, 228, 133, 56, 35, 185,
    200, 37, 120, 37, 30, 92, 31, 98, 76, 140, 181, 149, 217, 254, 171, 24, 253, 71, 208, 22, 248,
    210, 68, 10, 201, 20, 231, 97, 232, 251, 21, 254, 209, 36, 121, 186, 50, 213, 50, 77, 56, 154,
    188, 198, 175, 160, 241, 46, 1, 220, 142, 185, 106, 199, 178, 1, 232, 152, 244, 69, 253, 105,
    123, 251, 104, 151, 212, 140, 255, 124, 59, 228, 103, 172, 39, 109, 150, 171, 176, 184, 104, 28,
    166, 152, 87, 121, 63, 39, 112, 132, 243, 21, 178, 179, 177, 25, 225, 211, 108, 120, 145, 161,
    120, 49, 235, 233, 87, 203, 86, 99, 3, 138, 169, 241, 134, 236, 31, 156, 79, 17, 56, 2, 214, 147,
    101, 55, 61, 161, 189, 40, 146, 250, 31, 5, 243, 149, 117, 23, 228, 127, 34, 240, 120, 211, 109,
    75, 30, 51, 85, 166, 150, 211, 11, 166, 192, 164, 103, 11, 10, 175, 92, 35, 220, 18, 5, 63, 231,
    203, 203, 113, 9, 229, 151, 170, 50, 123, 8, 43, 29, 244, 151, 138, 243, 117, 159, 70, 63, 1,
    205, 31, 244, 135, 73, 178, 67, 230, 43, 90, 182, 115, 111, 127, 191, 30, 131, 200, 160, 35, 186,
    152, 210, 76, 217, 27
  ];

  private static readonly byte[] Rs256Exponent = [1, 0, 1];

  private static readonly byte[] Rs256D =
  [
    107, 139, 89, 163, 61, 189, 178, 205, 143, 29, 38, 74, 255, 102, 189, 99, 100, 230, 119, 28, 192,
    78, 164, 72, 89, 201, 56, 209, 198, 220, 194, 221, 170, 107, 96, 125, 236, 150, 40, 243, 37, 161,
    25, 87, 186, 109, 114, 209, 100, 69, 241, 66, 142, 199, 117, 121, 232, 64, 24, 173, 47, 132, 43,
    207, 76, 146, 180, 36, 220, 3, 14, 204, 109, 107, 160, 161, 93, 249, 158, 198, 11, 135, 70, 186,
    94, 53, 130, 54, 52, 69, 225, 86, 153, 134, 197, 98, 89, 230, 167, 190, 185, 81, 46, 162, 140,
    40, 27, 128, 202, 26, 149, 164, 224, 173, 232, 45, 221, 221, 15, 193, 43, 90, 116, 145, 150, 137,
    215, 61, 189, 145, 206, 148, 196, 214, 42, 140, 23, 57, 249, 29, 245, 7, 157, 24, 233, 108, 35,
    192, 236, 255, 71, 135, 120, 250, 175, 211, 182, 192, 88, 81, 92, 95, 23, 166, 184, 215, 190,
    123, 169, 71, 169, 1, 7, 240, 129, 120, 144, 52, 27, 69, 95, 33, 29, 117, 5, 50, 1, 25, 174, 0,
    117, 133, 90, 178, 133, 64, 200, 32, 143, 27, 234, 242, 206, 113, 128, 15, 81, 90, 222, 4, 2, 1,
    247, 62, 17, 84, 82, 76, 95, 91, 56, 38, 226, 181, 30, 74, 86, 37, 60, 17, 186, 148, 68, 77, 191,
    39, 243, 104, 175, 35, 205, 30, 138, 47, 0, 38, 125, 116, 127, 3, 211, 138, 182, 107, 216, 129
  ];

  private static readonly byte[] Rs256P =
  [
    254, 186, 137, 29, 223, 193, 149, 209, 44, 120, 208, 255, 26, 128, 233, 38, 160, 93, 130, 50,
    152, 155, 73, 198, 139, 191, 227, 144, 168, 189, 242, 34, 213, 86, 28, 78, 13, 136, 30, 231, 116,
    25, 158, 202, 26, 75, 232, 101, 74, 98, 200, 213, 76, 13, 190, 63, 221, 199, 87, 212, 254, 103,
    154, 149, 215, 202, 131, 75, 48, 70, 192, 165, 22, 183, 63, 207, 224, 132, 114, 208, 209, 47,
    159, 76, 247, 155, 53, 27, 36, 64, 30, 230, 97, 152, 23, 234, 202, 203, 142, 156, 216, 71, 241,
    90, 95, 73, 38, 108, 67, 40, 114, 148, 138, 248, 246, 143, 254, 131, 128, 136, 106, 122, 77, 96,
    183, 90, 15, 89
  ];

  private static readonly byte[] Rs256Q =
  [
    251, 229, 84, 1, 151, 88, 13, 34, 2, 164, 217, 141, 46, 114, 185, 1, 245, 34, 112, 187, 121, 87,
    86, 46, 125, 41, 114, 144, 149, 191, 235, 158, 184, 222, 250, 38, 176, 38, 95, 178, 214, 212, 90,
    2, 215, 125, 252, 146, 94, 240, 224, 7, 130, 6, 92, 252, 157, 229, 127, 194, 192, 19, 167, 201,
    1, 135, 103, 123, 31, 156, 149, 43, 96, 220, 219, 250, 180, 174, 103, 225, 130, 216, 196, 114,
    202, 66, 187, 137, 68, 216, 203, 12, 38, 154, 40, 254, 81, 228, 55, 105, 92, 231, 62, 140, 107,
    200, 161, 93, 73, 14, 101, 31, 163, 216, 166, 29, 45, 111, 253, 213, 139, 116, 23, 93, 115, 22,
    49, 147
  ];

  private static readonly byte[] Rs256DP =
  [
    221, 208, 183, 200, 32, 233, 229, 57, 49, 253, 191, 24, 246, 14, 93, 120, 250, 90, 147, 30, 214,
    15, 27, 174, 94, 81, 105, 171, 181, 149, 58, 62, 37, 2, 8, 65, 219, 188, 182, 20, 156, 224, 22,
    139, 45, 92, 254, 112, 253, 214, 137, 198, 91, 164, 248, 15, 139, 99, 164, 83, 96, 121, 253, 126,
    16, 92, 83, 250, 108, 126, 160, 16, 226, 120, 14, 132, 73, 161, 108, 141, 244, 43, 1, 16, 55,
    233, 154, 212, 24, 188, 17, 108, 82, 125, 236, 13, 212, 44, 111, 242, 154, 208, 3, 22, 204, 52,
    217, 213, 154, 161, 165, 45, 62, 219, 79, 113, 210, 146, 214, 161, 115, 255, 46, 84, 83, 53, 132,
    121
  ];

  private static readonly byte[] Rs256DQ =
  [
    121, 66, 187, 191, 12, 89, 81, 241, 38, 110, 175, 95, 252, 149, 51, 164, 210, 154, 34, 196, 205,
    52, 19, 3, 204, 50, 240, 184, 211, 174, 17, 66, 86, 98, 216, 239, 88, 235, 16, 52, 170, 160, 141,
    56, 66, 254, 158, 96, 228, 29, 118, 235, 134, 87, 131, 218, 4, 52, 223, 221, 35, 212, 18, 120,
    124, 40, 239, 210, 224, 179, 227, 71, 127, 152, 178, 185, 44, 211, 172, 164, 109, 245, 230, 20,
    16, 116, 49, 141, 114, 60, 30, 251, 25, 118, 42, 247, 202, 250, 95, 6, 116, 183, 201, 111, 149,
    207, 126, 134, 198, 205, 140, 54, 192, 12, 98, 99, 55, 101, 107, 63, 170, 163, 87, 64, 32, 79,
    245, 203
  ];

  private static readonly byte[] Rs256InverseQ =
  [
    151, 88, 177, 239, 81, 169, 26, 197, 43, 138, 76, 9, 23, 101, 57, 188, 194, 49, 94, 214, 147,
    164, 221, 241, 143, 236, 253, 75, 215, 55, 129, 115, 78, 61, 85, 105, 93, 79, 150, 139, 51, 186,
    180, 250, 129, 140, 192, 3, 245, 186, 26, 172, 190, 126, 50, 17, 141, 221, 221, 109, 226, 243,
    158, 213, 36, 79, 223, 169, 151, 42, 84, 97, 31, 29, 103, 6, 33, 45, 164, 61, 111, 158, 68, 95,
    194, 54, 67, 103, 54, 7, 2, 69, 19, 47, 26, 11, 43, 199, 158, 90, 90, 52, 58, 112, 27, 7, 130,
    201, 37, 51, 148, 149, 22, 111, 19, 167, 132, 16, 129, 193, 210, 110, 52, 223, 72, 181, 183, 251
  ];

  // Fixed, not random: reproducible attestedCredentialData across every test run.
  public static readonly byte[] FixedCredentialId = [1, 2, 3, 4, 5, 6, 7, 8];

  private readonly bool UseRsa;

  public SoftwareAuthenticator(bool useRsa = false)
  {
    UseRsa = useRsa;
  }

  public byte[] CredentialId => FixedCredentialId;

  public byte[] CosePublicKey => UseRsa ? BuildRsaCoseKey(Rs256Modulus, Rs256Exponent) : BuildEc2CoseKey(Es256X, Es256Y);

  public static byte[] BuildClientDataJson(string type, byte[] challenge, string origin) =>
    JsonSerializer.SerializeToUtf8Bytes(new { type, challenge = Base64Url.EncodeToString(challenge), origin });

  public byte[] BuildAuthenticatorData
  (
    string rpId,
    bool userPresent = true,
    bool userVerified = true,
    bool includeAttestedCredentialData = false,
    uint signCount = 0,
    byte[]? cosePublicKeyOverride = null
  )
  {
    byte[] rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));

    byte flags = 0;
    if (userPresent) flags |= 0x01;
    if (userVerified) flags |= 0x04;
    if (includeAttestedCredentialData) flags |= 0x40;

    byte[] signCountBytes = new byte[4];
    BinaryPrimitives.WriteUInt32BigEndian(signCountBytes, signCount);

    if (!includeAttestedCredentialData)
    {
      return [.. rpIdHash, flags, .. signCountBytes];
    }

    byte[] aaguid = new byte[16];
    byte[] credentialIdLength = new byte[2];
    BinaryPrimitives.WriteUInt16BigEndian(credentialIdLength, (ushort)CredentialId.Length);
    byte[] cosePublicKey = cosePublicKeyOverride ?? CosePublicKey;

    return [.. rpIdHash, flags, .. signCountBytes, .. aaguid, .. credentialIdLength, .. CredentialId, .. cosePublicKey];
  }

  public static byte[] BuildAttestationObject(byte[] authenticatorData, string fmt = "none", bool garbageAttStmt = false)
  {
    var writer = new CborWriter();
    writer.WriteStartMap(3);

    writer.WriteTextString("fmt");
    writer.WriteTextString(fmt);

    writer.WriteTextString("attStmt");
    if (garbageAttStmt)
    {
      writer.WriteStartMap(1);
      writer.WriteTextString("sig");
      writer.WriteByteString([0xDE, 0xAD, 0xBE, 0xEF]);
      writer.WriteEndMap();
    }
    else
    {
      writer.WriteStartMap(0);
      writer.WriteEndMap();
    }

    writer.WriteTextString("authData");
    writer.WriteByteString(authenticatorData);

    writer.WriteEndMap();
    return writer.Encode();
  }

  public byte[] Sign(byte[] authenticatorData, byte[] clientDataJson)
  {
    byte[] clientDataHash = SHA256.HashData(clientDataJson);
    byte[] signedData = [.. authenticatorData, .. clientDataHash];

    if (UseRsa)
    {
      using RSA rsa = CreateRsa();
      return rsa.SignData(signedData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    using ECDsa ecdsa = CreateEcdsa();
    return ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
  }

  /// <summary>
  /// Builds an otherwise-valid EC2/P-256 COSE_Key CBOR map (parses fine, would import fine)
  /// advertising alg -8 (EdDSA) instead of -7 (ES256) — isolates "algorithm not accepted" from
  /// "key structurally unparseable"/CoseKey.TryParse failing outright (kty=1/OKP is neither EC2 nor
  /// RSA, so CoseKey.TryParse itself would return false and the failure would be MalformedCoseKey,
  /// not UnsupportedAlgorithm).
  /// </summary>
  public static byte[] BuildUnsupportedAlgorithmCoseKey()
  {
    var writer = new CborWriter();
    writer.WriteStartMap(5);
    writer.WriteInt32(1);
    writer.WriteInt32(2); // kty: EC2
    writer.WriteInt32(3);
    writer.WriteInt32(-8); // alg: EdDSA (unsupported)
    writer.WriteInt32(-1);
    writer.WriteInt32(1); // crv: P-256
    writer.WriteInt32(-2);
    writer.WriteByteString(Es256X);
    writer.WriteInt32(-3);
    writer.WriteByteString(Es256Y);
    writer.WriteEndMap();
    return writer.Encode();
  }

  private static ECDsa CreateEcdsa()
  {
    ECParameters parameters = new()
    {
      Curve = ECCurve.NamedCurves.nistP256,
      D = Es256D,
      Q = new ECPoint { X = Es256X, Y = Es256Y }
    };

    return ECDsa.Create(parameters);
  }

  private static RSA CreateRsa()
  {
    RSA rsa = RSA.Create();
    rsa.ImportParameters(new RSAParameters
    {
      Modulus = Rs256Modulus,
      Exponent = Rs256Exponent,
      D = Rs256D,
      P = Rs256P,
      Q = Rs256Q,
      DP = Rs256DP,
      DQ = Rs256DQ,
      InverseQ = Rs256InverseQ
    });

    return rsa;
  }

  private static byte[] BuildEc2CoseKey(byte[] x, byte[] y)
  {
    var writer = new CborWriter();
    writer.WriteStartMap(5);
    writer.WriteInt32(1);
    writer.WriteInt32(2); // kty: EC2
    writer.WriteInt32(3);
    writer.WriteInt32(-7); // alg: ES256
    writer.WriteInt32(-1);
    writer.WriteInt32(1); // crv: P-256
    writer.WriteInt32(-2);
    writer.WriteByteString(x);
    writer.WriteInt32(-3);
    writer.WriteByteString(y);
    writer.WriteEndMap();
    return writer.Encode();
  }

  private static byte[] BuildRsaCoseKey(byte[] modulus, byte[] exponent)
  {
    var writer = new CborWriter();
    writer.WriteStartMap(4);
    writer.WriteInt32(1);
    writer.WriteInt32(3); // kty: RSA
    writer.WriteInt32(3);
    writer.WriteInt32(-257); // alg: RS256
    writer.WriteInt32(-1);
    writer.WriteByteString(modulus); // n
    writer.WriteInt32(-2);
    writer.WriteByteString(exponent); // e
    writer.WriteEndMap();
    return writer.Encode();
  }
}
