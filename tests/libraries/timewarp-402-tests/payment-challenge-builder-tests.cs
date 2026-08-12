#region Purpose
// Challenge encode/decode round-trips and Ready-only Build guard (never 402 from bad config).
#endregion

namespace PaymentChallengeBuilder_;

public class BuildAndCodec
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildAndCodec>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";

  public static Task Build_emits_base64_json_with_accepts_and_resource()
  {
    PaymentOptions options = PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip", "$0.01");

    (PaymentRequiredPayload payload, string header) = PaymentChallengeBuilder.Build(options);

    payload.X402Version.ShouldBe(2);
    payload.Accepts.Count.ShouldBe(1);
    payload.Accepts[0].PayTo.ShouldBe(ValidPayTo);
    payload.Accepts[0].Price.ShouldBe("$0.01");
    payload.Resource!.Path.ShouldBe("/api/tip");

    string? json = PaymentChallengeBuilder.TryDecodeHeaderPayload(header);
    json.ShouldNotBeNull();
    json.ShouldContain("\"accepts\"");
    json.ShouldContain(ValidPayTo);
    return Task.CompletedTask;
  }

  public static Task Build_throws_when_disabled()
  {
    PaymentOptions options = PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip") with
    {
      Enabled = false,
    };

    Should.Throw<InvalidOperationException>(() => PaymentChallengeBuilder.Build(options));
    return Task.CompletedTask;
  }

  public static Task Encode_and_decode_round_trip()
  {
    var body = new { success = true, transaction = "0xroundtrip" };
    string header = PaymentChallengeBuilder.EncodeHeaderPayload(body);
    string? json = PaymentChallengeBuilder.TryDecodeHeaderPayload(header);
    json.ShouldNotBeNull();
    json.ShouldContain("0xroundtrip");
    return Task.CompletedTask;
  }

  public static Task TryDecode_returns_null_for_garbage()
  {
    PaymentChallengeBuilder.TryDecodeHeaderPayload("not-base64!!").ShouldBeNull();
    PaymentChallengeBuilder.TryDecodeHeaderPayload(null).ShouldBeNull();
    PaymentChallengeBuilder.TryDecodeHeaderPayload("").ShouldBeNull();
    return Task.CompletedTask;
  }
}
