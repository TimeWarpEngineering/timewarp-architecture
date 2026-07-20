// ReSharper disable InconsistentNaming
namespace AgentPublicKey_;

public class TryParse
{
  public void Happy_path_p256_succeeds()
  {
    var key = new SoftwareAgentKey();

    AgentPublicKey.TryParse(key.SpkiPublicKey, out byte[] keyId).ShouldBeTrue();
    keyId.ShouldBe(SHA256.HashData(key.SpkiPublicKey));
  }

  public void KeyId_is_sha256_of_the_exact_spki_bytes()
  {
    var key = new SoftwareAgentKey();
    byte[] spki = key.SpkiPublicKey;

    AgentPublicKey.TryParse(spki, out byte[] keyId).ShouldBeTrue();
    keyId.ShouldBe(SHA256.HashData(spki));
    keyId.Length.ShouldBe(32);
  }

  public void Empty_array_fails_without_throwing()
  {
    AgentPublicKey.TryParse([], out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Oversized_array_fails_without_throwing()
  {
    byte[] oversized = new byte[(2 * 1024) + 1];

    AgentPublicKey.TryParse(oversized, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Truncated_der_fails_without_throwing()
  {
    var key = new SoftwareAgentKey();
    byte[] truncated = key.SpkiPublicKey[..^10];

    AgentPublicKey.TryParse(truncated, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Severely_truncated_der_fails_without_throwing()
  {
    var key = new SoftwareAgentKey();
    byte[] truncated = key.SpkiPublicKey[..3];

    AgentPublicKey.TryParse(truncated, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Trailing_bytes_fail_without_throwing()
  {
    var key = new SoftwareAgentKey();
    byte[] trailing = [.. key.SpkiPublicKey, 0xDE, 0xAD, 0xBE, 0xEF];

    AgentPublicKey.TryParse(trailing, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Garbage_bytes_fail_without_throwing()
  {
    byte[] garbage = [0xFF, 0xFF, 0xFF, 0x00, 0x01];

    AgentPublicKey.TryParse(garbage, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Rsa_spki_is_rejected()
  {
    AgentPublicKey.TryParse(SoftwareAgentKey.RsaSpki, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void P384_spki_is_rejected()
  {
    AgentPublicKey.TryParse(SoftwareAgentKey.P384Spki, out byte[] keyId).ShouldBeFalse();
    keyId.ShouldBeEmpty();
  }

  public void Distinct_keys_produce_distinct_key_ids()
  {
    var key1 = new SoftwareAgentKey();
    var key2 = new SoftwareAgentKey(useSecondKey: true);

    AgentPublicKey.TryParse(key1.SpkiPublicKey, out byte[] keyId1).ShouldBeTrue();
    AgentPublicKey.TryParse(key2.SpkiPublicKey, out byte[] keyId2).ShouldBeTrue();

    keyId1.ShouldNotBe(keyId2);
  }
}
