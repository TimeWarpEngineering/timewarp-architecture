// ReSharper disable InconsistentNaming
namespace AgentKeyProof_;

public class Verify
{
  public void Happy_path_registration_succeeds()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, challenge, signature);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.None);
  }

  public void Happy_path_token_issuance_succeeds()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.TokenIssuance, key.SpkiPublicKey, challenge, signature);

    result.IsValid.ShouldBeTrue();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.None);
  }

  public void Tampered_signature_fails()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);
    signature[0] ^= 0xFF;

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, challenge, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Wrong_challenge_fails()
  {
    var key = new SoftwareAgentKey();
    byte[] signedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] expectedChallenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, signedChallenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, expectedChallenge, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Cross_ceremony_replay_fails()
  {
    // Domain separation (task 104-004 §1): a signature produced for Registration must not verify
    // for TokenIssuance, even presented with the SAME challenge value — the signed bytes differ
    // because the prefix differs.
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] registrationSignature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.TokenIssuance, key.SpkiPublicKey, challenge, registrationSignature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Cross_ceremony_replay_fails_the_other_direction()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, challenge, tokenSignature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Wrong_key_fails()
  {
    var signer = new SoftwareAgentKey(useSecondKey: true);
    var claimedKey = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = signer.Sign(AgentKeyCeremonyType.Registration, challenge);

    // Signature made by the SECOND key, presented against the FIRST key's public material.
    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, claimedKey.SpkiPublicKey, challenge, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Empty_public_key_fails_without_throwing()
  {
    byte[] challenge = RandomNumberGenerator.GetBytes(32);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, [], challenge, [1, 2, 3]);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.MalformedPublicKey);
  }

  public void Empty_signature_fails_without_throwing()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, challenge, []);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Empty_challenge_fails_without_throwing()
  {
    var key = new SoftwareAgentKey();

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, key.SpkiPublicKey, [], [1, 2, 3]);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }

  public void Rsa_public_key_is_rejected_as_malformed()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, SoftwareAgentKey.RsaSpki, challenge, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.MalformedPublicKey);
  }

  public void P384_public_key_is_rejected_as_unsupported_algorithm()
  {
    var key = new SoftwareAgentKey();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, SoftwareAgentKey.P384Spki, challenge, signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.UnsupportedAlgorithm);
  }

  public void P1363_signature_format_is_rejected()
  {
    // Only DER (Rfc3279DerSequence) is accepted — P1363 (raw r‖s) must fail even against the
    // CORRECT key and challenge, since it is cryptographically the "same" signature just differently
    // encoded (task 104-004 §1: no dual-format malleability surface). Uses a fresh, non-fixture key
    // (determinism is not needed here — the vector only asserts "P1363 format is rejected", not any
    // specific byte content) so both the signature and the public key it is checked against agree,
    // isolating the format rejection from an unrelated "wrong key" failure.
    using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] signedData = AgentKeyProof.BuildSignedData(AgentKeyCeremonyType.Registration, challenge);
    byte[] p1363Signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

    AgentKeyProofResult result = AgentKeyProof.Verify(AgentKeyCeremonyType.Registration, spki, challenge, p1363Signature);

    result.IsValid.ShouldBeFalse();
    result.FailureReason.ShouldBe(AgentKeyFailureReason.SignatureInvalid);
  }
}
