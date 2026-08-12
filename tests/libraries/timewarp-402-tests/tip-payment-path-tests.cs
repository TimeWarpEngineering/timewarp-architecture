#region Purpose
// Library-level voluntary tip path (402 / 503 / 200) with mock facilitator — host tip jar is 104-009.
#endregion

#region Design
// 104-009 tip host is still open; Wave 2 package exit (104-012) covers the same PaymentGate policy
// against a tip-shaped resource (/api/tip, tip price/description) so CI proves tip economics without
// live chain or the tip host. Residual: host free-route isolation + tip endpoint wiring stay on 009.
#endregion

namespace TipPaymentPath_;

using TimeWarp.X402.TestSupport;

public class EvaluateAsync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EvaluateAsync>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";

  public static async Task Tip_disabled_is_unavailable_503_not_402()
  {
    PaymentGate gate = new(new MockFacilitator());

    PaymentGateOutcome outcome = await gate.EvaluateAsync(
      TipOptions() with { Enabled = false },
      paymentSignatureHeader: null);

    PaymentUnavailable unavailable = outcome.ShouldBeOfType<PaymentUnavailable>();
    unavailable.Status.ShouldBe(PaymentConfigStatus.Disabled);
    PaymentErrorPayload body = unavailable.ToErrorPayload();
    body.Error.ShouldBe(PaymentConfigEvaluator.ErrorDisabled);
    body.Payment.ShouldBeTrue();
    body.Ok.ShouldBeFalse();
  }

  public static async Task Tip_unpaid_is_challenge_402()
  {
    PaymentGate gate = new(new MockFacilitator());

    PaymentGateOutcome outcome = await gate.EvaluateAsync(TipOptions(), paymentSignatureHeader: null);

    PaymentChallenge challenge = outcome.ShouldBeOfType<PaymentChallenge>();
    challenge.Payload.Resource!.Path.ShouldBe("/api/tip");
    challenge.Payload.Accepts[0].Price.ShouldBe("$0.01");
    challenge.Payload.Accepts[0].Description.ShouldBe("Voluntary tip");
    challenge.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
  }

  public static async Task Tip_settled_is_success_200_with_payment_response()
  {
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = "0xtip-settle-1",
        Network = "eip155:84532",
        Payer = ValidPayTo,
      },
    };
    PaymentGate gate = new(facilitator);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    PaymentGateOutcome outcome = await gate.EvaluateAsync(TipOptions(), signature);

    PaymentSettled settled = outcome.ShouldBeOfType<PaymentSettled>();
    settled.Result.Transaction.ShouldBe("0xtip-settle-1");
    settled.PaymentResponseHeader.ShouldNotBeNullOrWhiteSpace();
    string? responseJson = PaymentChallengeBuilder.TryDecodeHeaderPayload(settled.PaymentResponseHeader);
    responseJson.ShouldNotBeNull();
    responseJson.ShouldContain("0xtip-settle-1");
    facilitator.VerifyCalls.ShouldBe(1);
    facilitator.SettleCalls.ShouldBe(1);
  }

  public static async Task Tip_rejected_payment_stays_402_with_fresh_challenge()
  {
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = false, InvalidReason = "tip_invalid" },
    };
    PaymentGate gate = new(facilitator);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2 });

    PaymentGateOutcome outcome = await gate.EvaluateAsync(TipOptions(), signature);

    PaymentRejected rejected = outcome.ShouldBeOfType<PaymentRejected>();
    rejected.Reason.ShouldBe("tip_invalid");
    rejected.Payload.Resource!.Path.ShouldBe("/api/tip");
    rejected.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
  }

  private static PaymentOptions TipOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip", price: "$0.01") with
    {
      Description = "Voluntary tip",
    };
}
