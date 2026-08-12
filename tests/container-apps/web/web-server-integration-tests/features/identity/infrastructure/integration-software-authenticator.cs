#region Purpose
// Deterministic software WebAuthn authenticator for the identity feature's HTTP-level integration
// tests: builds spec-correct clientDataJSON/authenticatorData/attestationObject and signs
// assertions, using the SAME fixed ES256 test keypair as the library's unit-test fixture.
#endregion

#region Design
// Deliberate duplication of tests/libraries/timewarp-identity-tests/ceremonies/infrastructure/
// software-authenticator.cs rather than a shared reference: that fixture is `internal` to the
// timewarp-identity-tests assembly (a different assembly than this project), and the alternative —
// exposing it publicly from a library test assembly, or adding an InternalsVisibleTo just to reach
// across the tests/ tree — would leak a test-only concern across assembly boundaries for the sake of
// ~150 lines. Only the ES256 keypair is reproduced (the RSA fixture is intentionally omitted — the
// unit tests already cover the RS256 signature-verification path; nothing here needs a second
// algorithm). Unlike the unit fixture, the challenge is never a fixture constant: every test reads
// it from a live StartPasskeyRegistration/StartPasskeyAuthentication response, matching a real
// browser ceremony against the running host.
// CredentialId is per-instance random here, NOT the unit fixture's fixed constant: this test project's
// WebTestServerApplication (and its in-memory IPrincipalStore/IWebAuthnChallengeStore singletons) is
// constructed once and shared across every test method in a class (observed: a single "WebApplication
// Started"/dispose pair per class, not per method), so a hardcoded CredentialId shared by every
// `new IntegrationSoftwareAuthenticator()` would collide across test methods within the same class —
// a credential registered by one test's happy path would already exist when a later test's "unknown
// credential" case expects it to be absent. The signing keypair stays fixed (that is what "no
// Date.now-style nondeterminism" protects: signature verification must be reproducible); CredentialId
// is just an opaque handle, and real authenticators mint a fresh one per registration anyway.
#endregion

namespace TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal sealed class IntegrationSoftwareAuthenticator
{
  // Fixed ES256 (P-256) test keypair — identical to the unit-test fixture's, but this is a
  // deliberate duplicate (see Design region), not a shared constant.
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

  public byte[] CredentialId { get; } = RandomNumberGenerator.GetBytes(16);

  public byte[] CosePublicKey => BuildEc2CoseKey(Es256X, Es256Y);

  public static byte[] BuildClientDataJson(string type, byte[] challenge, string origin) =>
    JsonSerializer.SerializeToUtf8Bytes(new { type, challenge = Base64Url.EncodeToString(challenge), origin });

  public byte[] BuildAuthenticatorData
  (
    string rpId,
    bool userPresent = true,
    bool userVerified = true,
    bool includeAttestedCredentialData = false,
    uint signCount = 0
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

    return [.. rpIdHash, flags, .. signCountBytes, .. aaguid, .. credentialIdLength, .. CredentialId, .. CosePublicKey];
  }

  public static byte[] BuildAttestationObject(byte[] authenticatorData, string fmt = "none")
  {
    var writer = new CborWriter();
    writer.WriteStartMap(3);

    writer.WriteTextString("fmt");
    writer.WriteTextString(fmt);

    writer.WriteTextString("attStmt");
    writer.WriteStartMap(0);
    writer.WriteEndMap();

    writer.WriteTextString("authData");
    writer.WriteByteString(authenticatorData);

    writer.WriteEndMap();
    return writer.Encode();
  }

  public byte[] Sign(byte[] authenticatorData, byte[] clientDataJson)
  {
    byte[] clientDataHash = SHA256.HashData(clientDataJson);
    byte[] signedData = [.. authenticatorData, .. clientDataHash];

    using ECDsa ecdsa = CreateEcdsa();
    return ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
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
}
