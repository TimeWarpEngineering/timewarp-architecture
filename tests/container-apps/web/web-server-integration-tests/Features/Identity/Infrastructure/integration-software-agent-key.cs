#region Purpose
// Deterministic-shape (but per-instance-random) software agent key for the identity feature's
// HTTP-level integration tests — the agent-key analog of IntegrationSoftwareAuthenticator.
#endregion

#region Design
// Deliberate duplication of tests/libraries/timewarp-identity-tests/ceremonies/infrastructure/
// software-agent-key.cs rather than a shared reference — same rationale as
// IntegrationSoftwareAuthenticator's Design region (104-003): that fixture is `internal` to a
// different assembly, and the alternative (exposing it publicly, or an InternalsVisibleTo just to
// reach across the tests/ tree) would leak a test-only concern across an assembly boundary for a
// small fixture.
// KeyId is per-instance random from day one here (NOT a fixed constant) — 104-003's round-1 review
// (finding M6) caught the passkey integration fixture using a fixed CredentialId and colliding
// across test methods within the same class, because WebTestServerApplication (and its in-memory
// IPrincipalStore singleton) is constructed once and SHARED across every test method in a Fixie test
// class, not fresh per method. Applying that lesson from day one: each `new IntegrationSoftwareAgentKey()`
// generates its own P-256 keypair at construction time, so two different test methods registering
// "a" key can never collide on KeyId (SHA-256 of the public key) the way a fixed key would.
#endregion

namespace TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

using System.Security.Cryptography;
using TimeWarp.Identity;

internal sealed class IntegrationSoftwareAgentKey
{
  private readonly byte[] D;
  private readonly byte[] X;
  private readonly byte[] Y;

  public IntegrationSoftwareAgentKey()
  {
    using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    ECParameters parameters = ecdsa.ExportParameters(includePrivateParameters: true);
    D = parameters.D!;
    X = parameters.Q.X!;
    Y = parameters.Q.Y!;
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

  private ECDsa CreateEcdsa() => ECDsa.Create(new ECParameters
  {
    Curve = ECCurve.NamedCurves.nistP256,
    D = D,
    Q = new ECPoint { X = X, Y = Y }
  });
}
