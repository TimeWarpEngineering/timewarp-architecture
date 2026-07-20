// ReSharper disable InconsistentNaming
namespace WebAuthnChallengeReader_;

public class TryReadChallenge
{
  public void Reads_the_challenge_from_valid_clientDataJson()
  {
    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    byte[] clientDataJson = SoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, "https://localhost:7000");

    WebAuthnChallengeReader.TryReadChallenge(clientDataJson, out byte[] result).ShouldBeTrue();
    result.ShouldBe(challenge);
  }

  public void Rejects_malformed_json()
  {
    byte[] garbage = "not json"u8.ToArray();

    WebAuthnChallengeReader.TryReadChallenge(garbage, out byte[] result).ShouldBeFalse();
    result.ShouldBeEmpty();
  }

  public void Rejects_missing_challenge_field()
  {
    byte[] clientDataJson = JsonSerializer.SerializeToUtf8Bytes(new { type = "webauthn.create", origin = "https://localhost:7000" });

    WebAuthnChallengeReader.TryReadChallenge(clientDataJson, out byte[] result).ShouldBeFalse();
    result.ShouldBeEmpty();
  }

  public void Rejects_non_base64url_challenge()
  {
    byte[] clientDataJson = JsonSerializer.SerializeToUtf8Bytes(new
    {
      type = "webauthn.create",
      challenge = "not-valid-base64url!!!",
      origin = "https://localhost:7000"
    });

    WebAuthnChallengeReader.TryReadChallenge(clientDataJson, out byte[] result).ShouldBeFalse();
    result.ShouldBeEmpty();
  }

  public void Rejects_empty_challenge_field()
  {
    byte[] clientDataJson = JsonSerializer.SerializeToUtf8Bytes(new
    {
      type = "webauthn.create",
      challenge = "",
      origin = "https://localhost:7000"
    });

    WebAuthnChallengeReader.TryReadChallenge(clientDataJson, out byte[] result).ShouldBeFalse();
    result.ShouldBeEmpty();
  }
}
