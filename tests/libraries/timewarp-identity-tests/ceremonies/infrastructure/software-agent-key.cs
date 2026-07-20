#region Purpose
// Deterministic software agent key: builds spec-correct SPKI DER public keys and signs
// domain-separated agent-key proofs, using FIXED test-only ECDSA P-256 keypairs (never randomly
// generated per run) — the agent-key analog of SoftwareAuthenticator.
#endregion

#region Design
// Two fixed P-256 keypairs (generated once, offline, via a throwaway .NET file-based app — same
// process as SoftwareAuthenticator's ES256 fixture): the primary key is what most vectors sign
// with/register; the second key exists solely for "signed by the wrong key" vectors (a signature
// that is well-formed DER but does not verify against the credential's stored public key).
// RsaSpki/P384Spki are canned bad material — a real RSA-2048 and a real P-384 public key, each
// exported as genuine SubjectPublicKeyInfo DER — so "wrong algorithm"/"wrong curve" test vectors
// exercise AgentPublicKey/AgentKeyProof against ACTUAL structurally-valid-but-unaccepted keys, not
// synthetic garbage (which a different code path — MalformedPublicKey via CryptographicException on
// import failure — would exercise instead; RSA and P-384 both IMPORT successfully via
// ECDsa/RSA.ImportSubjectPublicKeyInfo, so they specifically probe the curve/algorithm-acceptance
// gate, not the parse gate).
#endregion

namespace TimeWarp.Identity.Tests.Ceremonies.Infrastructure;

internal sealed class SoftwareAgentKey
{
  // Fixed primary P-256 (ES256) test keypair.
  private static readonly byte[] D =
  [
    33, 1, 93, 211, 59, 73, 116, 115, 113, 150, 45, 29, 156, 205, 101, 86, 39, 152, 134, 109, 47,
    147, 167, 94, 178, 60, 218, 240, 11, 184, 64, 65
  ];

  private static readonly byte[] X =
  [
    180, 177, 31, 130, 195, 189, 180, 230, 198, 20, 144, 225, 69, 166, 166, 87, 184, 41, 254, 96,
    116, 53, 251, 25, 197, 117, 252, 59, 79, 50, 130, 80
  ];

  private static readonly byte[] Y =
  [
    27, 36, 194, 47, 116, 155, 239, 82, 38, 50, 22, 93, 175, 230, 248, 4, 104, 51, 210, 198, 66, 53,
    54, 76, 209, 207, 93, 98, 242, 60, 177, 178
  ];

  // Fixed SECOND P-256 test keypair — used only for "signed by the wrong key" vectors.
  private static readonly byte[] D2 =
  [
    120, 126, 203, 113, 222, 115, 230, 241, 53, 195, 169, 5, 128, 211, 49, 237, 150, 176, 126, 183,
    206, 179, 132, 197, 107, 29, 43, 33, 114, 232, 75, 214
  ];

  private static readonly byte[] X2 =
  [
    71, 63, 39, 228, 137, 180, 185, 240, 187, 244, 139, 28, 227, 67, 136, 131, 240, 173, 118, 249,
    205, 140, 111, 239, 194, 92, 145, 156, 114, 107, 20, 206
  ];

  private static readonly byte[] Y2 =
  [
    87, 42, 218, 96, 230, 209, 231, 221, 186, 41, 82, 135, 135, 143, 15, 52, 9, 182, 184, 86, 216,
    105, 245, 234, 14, 97, 240, 115, 1, 28, 184, 175
  ];

  // Canned bad material: a real RSA-2048 public key, SPKI DER (wrong algorithm; length 294).
  public static readonly byte[] RsaSpki =
  [
    48, 130, 1, 34, 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 1, 5, 0, 3, 130, 1, 15, 0, 48, 130,
    1, 10, 2, 130, 1, 1, 0, 195, 120, 180, 26, 88, 28, 168, 38, 10, 86, 191, 183, 229, 56, 115, 33,
    142, 151, 24, 172, 89, 157, 224, 120, 78, 108, 232, 174, 169, 50, 234, 70, 77, 103, 111, 239, 142,
    5, 50, 73, 166, 209, 167, 218, 216, 156, 158, 44, 130, 155, 103, 189, 208, 127, 6, 69, 227, 208,
    95, 157, 141, 119, 109, 165, 169, 126, 210, 39, 239, 8, 211, 232, 113, 72, 166, 66, 190, 129, 99,
    131, 38, 41, 242, 115, 37, 17, 5, 167, 237, 158, 65, 153, 44, 30, 92, 71, 187, 66, 88, 7, 154,
    230, 16, 114, 70, 188, 106, 173, 195, 213, 18, 249, 75, 145, 126, 253, 161, 32, 60, 230, 70, 121,
    122, 223, 108, 140, 71, 242, 48, 167, 235, 194, 50, 238, 209, 113, 79, 43, 186, 38, 66, 19, 32,
    251, 196, 208, 238, 246, 220, 252, 242, 76, 167, 161, 38, 156, 252, 205, 210, 166, 49, 44, 122,
    237, 199, 249, 79, 222, 151, 179, 162, 213, 7, 37, 21, 233, 21, 125, 101, 86, 140, 171, 100, 107,
    181, 135, 251, 49, 62, 92, 218, 6, 28, 219, 124, 170, 42, 249, 65, 247, 86, 92, 16, 15, 26, 2,
    118, 173, 93, 102, 241, 233, 207, 102, 21, 129, 171, 59, 52, 108, 253, 202, 61, 78, 238, 23, 58,
    118, 237, 43, 178, 68, 209, 229, 70, 202, 186, 146, 68, 203, 107, 40, 2, 37, 145, 154, 81, 59,
    241, 60, 151, 144, 47, 122, 47, 63, 2, 3, 1, 0, 1
  ];

  // Canned bad material: a real P-384 public key, SPKI DER (wrong curve; length 120).
  public static readonly byte[] P384Spki =
  [
    48, 118, 48, 16, 6, 7, 42, 134, 72, 206, 61, 2, 1, 6, 5, 43, 129, 4, 0, 34, 3, 98, 0, 4, 102, 42,
    240, 117, 226, 179, 183, 64, 208, 200, 37, 255, 46, 106, 11, 42, 53, 34, 25, 130, 62, 158, 51,
    129, 226, 10, 161, 104, 59, 128, 130, 178, 145, 220, 63, 118, 47, 54, 158, 88, 170, 158, 122,
    255, 78, 172, 14, 19, 225, 214, 233, 187, 65, 230, 36, 69, 12, 42, 244, 15, 199, 180, 29, 139,
    249, 153, 205, 140, 196, 168, 102, 183, 83, 250, 128, 183, 45, 114, 219, 242, 47, 167, 103, 251,
    151, 249, 183, 7, 25, 135, 23, 105, 198, 28, 128, 200
  ];

  private readonly bool UseSecondKey;

  public SoftwareAgentKey(bool useSecondKey = false)
  {
    UseSecondKey = useSecondKey;
  }

  public byte[] SpkiPublicKey
  {
    get
    {
      using ECDsa ecdsa = CreateEcdsa();
      return ecdsa.ExportSubjectPublicKeyInfo();
    }
  }

  public byte[] KeyId => SHA256.HashData(SpkiPublicKey);

  public byte[] Sign(AgentKeyCeremonyType ceremonyType, byte[] challenge)
  {
    byte[] signedData = AgentKeyProof.BuildSignedData(ceremonyType, challenge);
    using ECDsa ecdsa = CreateEcdsa();
    return ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
  }

  private ECDsa CreateEcdsa()
  {
    ECParameters parameters = new()
    {
      Curve = ECCurve.NamedCurves.nistP256,
      D = UseSecondKey ? D2 : D,
      Q = new ECPoint { X = UseSecondKey ? X2 : X, Y = UseSecondKey ? Y2 : Y }
    };

    return ECDsa.Create(parameters);
  }
}
