#region Purpose
// PaymentGate outcomes with a mock facilitator: 503 unavailable, 402 challenge/reject, 200 settle.
#endregion

#region Design
// Hosts map Unavailable→503, Challenge/Rejected→402, Settled→200. Free routes never call the gate.
// No live chain: MockFacilitator only. Tip resource path is exercised here and in tip-payment-path-tests.
#endregion

namespace PaymentGate_;

using TimeWarp.X402.TestSupport;

public class EvaluateAsync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EvaluateAsync>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";

  public static async Task Disabled_returns_unavailable_not_challenge()
  {
    PaymentGate gate = new(new MockFacilitator());
    PaymentOptions options = ReadyOptions() with { Enabled = false };

    PaymentGateOutcome outcome = await gate.EvaluateAsync(options, paymentSignatureHeader: null);

    outcome.ShouldBeOfType<PaymentUnavailable>();
    var unavailable = (PaymentUnavailable)outcome;
    unavailable.Status.ShouldBe(PaymentConfigStatus.Disabled);
    unavailable.ToErrorPayload().Error.ShouldBe(PaymentConfigEvaluator.ErrorDisabled);
    unavailable.ToErrorPayload().Payment.ShouldBeTrue();
  }

  public static async Task Misconfigured_returns_unavailable_not_challenge()
  {
    PaymentGate gate = new(new MockFacilitator());
    PaymentOptions options = ReadyOptions() with { PayTo = "0x0000000000000000000000000000000000000000" };

    PaymentGateOutcome outcome = await gate.EvaluateAsync(options, paymentSignatureHeader: null);

    PaymentUnavailable unavailable = outcome.ShouldBeOfType<PaymentUnavailable>();
    unavailable.Status.ShouldBe(PaymentConfigStatus.Misconfigured);
    unavailable.ErrorCode.ShouldBe(PaymentConfigEvaluator.ErrorMisconfigured);
  }

  public static async Task Unpaid_returns_challenge_with_payment_required_header()
  {
    PaymentGate gate = new(new MockFacilitator());
    PaymentOptions options = ReadyOptions();

    PaymentGateOutcome outcome = await gate.EvaluateAsync(options, paymentSignatureHeader: null);

    outcome.ShouldBeOfType<PaymentChallenge>();
    var challenge = (PaymentChallenge)outcome;
    challenge.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
    challenge.Payload.Accepts.Count.ShouldBe(1);
    challenge.Payload.Accepts[0].PayTo.ShouldBe(ValidPayTo);
    challenge.Payload.Accepts[0].Scheme.ShouldBe("exact");
    challenge.Payload.Resource!.Path.ShouldBe("/api/tip");

    string? json = PaymentChallengeBuilder.TryDecodeHeaderPayload(challenge.PaymentRequiredHeader);
    json.ShouldNotBeNull();
    json.ShouldContain("accepts");
  }

  public static async Task Valid_signature_and_facilitator_returns_settled()
  {
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = "0xabc123settled",
        Network = "eip155:84532",
        Payer = ValidPayTo,
      },
    };
    PaymentGate gate = new(facilitator);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    PaymentGateOutcome outcome = await gate.EvaluateAsync(ReadyOptions(), signature);

    outcome.ShouldBeOfType<PaymentSettled>();
    var settled = (PaymentSettled)outcome;
    settled.Result.Transaction.ShouldBe("0xabc123settled");
    settled.PaymentResponseHeader.ShouldNotBeNullOrWhiteSpace();
    facilitator.VerifyCalls.ShouldBe(1);
    facilitator.SettleCalls.ShouldBe(1);
    facilitator.LastVerifyRequest.ShouldNotBeNull();
    facilitator.LastSettleRequest.ShouldNotBeNull();
  }

  public static async Task Invalid_verify_returns_rejected_with_challenge()
  {
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = false, InvalidReason = "invalid_payload" },
    };
    PaymentGate gate = new(facilitator);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2 });

    PaymentGateOutcome outcome = await gate.EvaluateAsync(ReadyOptions(), signature);

    outcome.ShouldBeOfType<PaymentRejected>();
    var rejected = (PaymentRejected)outcome;
    rejected.Reason.ShouldBe("invalid_payload");
    rejected.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
    facilitator.SettleCalls.ShouldBe(0);
  }

  public static async Task Failed_settle_after_valid_verify_returns_rejected()
  {
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = false,
        ErrorReason = "insufficient_funds",
        Network = "eip155:84532",
      },
    };
    PaymentGate gate = new(facilitator);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    PaymentGateOutcome outcome = await gate.EvaluateAsync(ReadyOptions(), signature);

    PaymentRejected rejected = outcome.ShouldBeOfType<PaymentRejected>();
    rejected.Reason.ShouldBe("insufficient_funds");
    rejected.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
    facilitator.VerifyCalls.ShouldBe(1);
    facilitator.SettleCalls.ShouldBe(1);
  }

  public static async Task Malformed_signature_header_returns_rejected()
  {
    MockFacilitator facilitator = new();
    PaymentGate gate = new(facilitator);

    PaymentGateOutcome outcome = await gate.EvaluateAsync(ReadyOptions(), "%%%not-base64%%%");

    PaymentRejected rejected = outcome.ShouldBeOfType<PaymentRejected>();
    rejected.Reason.ShouldBe("malformed_payment_signature");
    facilitator.VerifyCalls.ShouldBe(0);
    facilitator.SettleCalls.ShouldBe(0);
  }

  public static async Task Malformed_signature_json_returns_rejected()
  {
    MockFacilitator facilitator = new();
    PaymentGate gate = new(facilitator);
    // Valid Base64 that is not JSON.
    string notJson = Convert.ToBase64String(Encoding.UTF8.GetBytes("not-json{"));

    PaymentGateOutcome outcome = await gate.EvaluateAsync(ReadyOptions(), notJson);

    PaymentRejected rejected = outcome.ShouldBeOfType<PaymentRejected>();
    rejected.Reason.ShouldBe("malformed_payment_signature_json");
    facilitator.VerifyCalls.ShouldBe(0);
  }

  private static PaymentOptions ReadyOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip");
}
