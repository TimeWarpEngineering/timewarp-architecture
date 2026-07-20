// ReSharper disable InconsistentNaming
namespace AgentSigning_;

public class BuildSignedData_And_Sign
{
  public void Registration_prefix_is_domain_separated()
  {
    byte[] challenge = [1, 2, 3, 4];
    byte[] signed = AgentKeyProof.BuildSignedData(AgentKeyCeremonyType.Registration, challenge);

    byte[] expectedPrefix = Encoding.UTF8.GetBytes("TimeWarp.Identity.AgentKey.Register.v1:");
    signed.AsSpan(0, expectedPrefix.Length).SequenceEqual(expectedPrefix).ShouldBeTrue();
    signed.AsSpan(expectedPrefix.Length).SequenceEqual(challenge).ShouldBeTrue();
  }

  public void Token_prefix_is_domain_separated()
  {
    byte[] challenge = [9, 8, 7, 6];
    byte[] signed = AgentKeyProof.BuildSignedData(AgentKeyCeremonyType.TokenIssuance, challenge);

    byte[] expectedPrefix = Encoding.UTF8.GetBytes("TimeWarp.Identity.AgentKey.Token.v1:");
    signed.AsSpan(0, expectedPrefix.Length).SequenceEqual(expectedPrefix).ShouldBeTrue();
    signed.AsSpan(expectedPrefix.Length).SequenceEqual(challenge).ShouldBeTrue();
  }

  public void Registration_and_Token_signed_data_differ_for_same_challenge()
  {
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] registration = AgentKeyProof.BuildSignedData(AgentKeyCeremonyType.Registration, challenge);
    byte[] token = AgentKeyProof.BuildSignedData(AgentKeyCeremonyType.TokenIssuance, challenge);

    registration.SequenceEqual(token).ShouldBeFalse();
  }

  public void Cli_Sign_registration_verifies_with_AgentKeyProof()
  {
    var signing = new AgentSigning();
    GeneratedKey generated = signing.GenerateKey();
    using ECDsa ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(generated.Pem);

    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = signing.Sign(ecdsa, AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(
      AgentKeyCeremonyType.Registration,
      generated.SpkiPublicKey,
      challenge,
      signature);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.None);
  }

  public void Registration_signature_fails_Token_verify()
  {
    var signing = new AgentSigning();
    GeneratedKey generated = signing.GenerateKey();
    using ECDsa ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(generated.Pem);

    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] registrationSignature = signing.Sign(ecdsa, AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(
      AgentKeyCeremonyType.TokenIssuance,
      generated.SpkiPublicKey,
      challenge,
      registrationSignature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Keygen_SPKI_parses_and_KeyId_matches_SHA256()
  {
    var signing = new AgentSigning();
    GeneratedKey generated = signing.GenerateKey();

    AgentPublicKey.TryParse(generated.SpkiPublicKey, out byte[] keyId).ShouldBeTrue();
    keyId.SequenceEqual(generated.KeyId).ShouldBeTrue();
    keyId.SequenceEqual(SHA256.HashData(generated.SpkiPublicKey)).ShouldBeTrue();

    // Wire encoding is base64url (not standard base64).
    string b64url = AgentSigning.ToBase64Url(generated.KeyId);
    b64url.ShouldNotContain("+");
    b64url.ShouldNotContain("/");
    AgentSigning.FromBase64Url(b64url).SequenceEqual(generated.KeyId).ShouldBeTrue();
  }

  public void LoadKey_round_trips_PEM_and_KeyId()
  {
    var signing = new AgentSigning();
    GeneratedKey generated = signing.GenerateKey();
    string path = Path.Combine(Path.GetTempPath(), $"agent-key-{Guid.NewGuid():N}.pem");

    try
    {
      signing.WriteKeyFile(path, generated.Pem, force: true);
      using LoadedKey loaded = signing.LoadKey(path);

      loaded.SpkiPublicKey.SequenceEqual(generated.SpkiPublicKey).ShouldBeTrue();
      loaded.KeyId.SequenceEqual(generated.KeyId).ShouldBeTrue();
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }
}
