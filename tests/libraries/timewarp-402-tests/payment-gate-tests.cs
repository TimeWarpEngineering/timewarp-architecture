#region Purpose
// PaymentGate outcomes: unavailable vs challenge vs settle with a mock facilitator.
#endregion

namespace PaymentGate_;

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

  private static PaymentOptions ReadyOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip");
}

/// <summary>In-test facilitator matching tip.test.js mockFacilitator shape.</summary>
internal sealed class MockFacilitator : IFacilitatorClient
{
  public FacilitatorVerifyResult VerifyResult { get; init; } =
    new() { IsValid = false, InvalidReason = "invalid_payload" };

  public FacilitatorSettleResult SettleResult { get; init; } =
    new() { Success = false, ErrorReason = "not_implemented", Network = "eip155:84532" };

  public int VerifyCalls { get; private set; }
  public int SettleCalls { get; private set; }

  public Task<FacilitatorSupported> GetSupportedAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(new FacilitatorSupported
    {
      Kinds =
      [
        new FacilitatorKind { X402Version = 2, Scheme = "exact", Network = "eip155:84532" },
      ],
    });

  public Task<FacilitatorVerifyResult> VerifyAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    VerifyCalls++;
    return Task.FromResult(VerifyResult);
  }

  public Task<FacilitatorSettleResult> SettleAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    SettleCalls++;
    return Task.FromResult(SettleResult);
  }
}
