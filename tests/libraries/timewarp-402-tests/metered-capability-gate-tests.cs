#region Purpose
// MeteredCapabilityGate: credit debit, 402 challenge, settle→credit→debit, disabled→unavailable.
#endregion

namespace MeteredCapabilityGate_;

using TimeWarp.Identity;
using TimeWarp.X402;

public class EvaluateAsync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EvaluateAsync>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";
  private static readonly PrincipalId Principal = PrincipalId.New();

  public static async Task Prepaid_credit_debits_without_facilitator()
  {
    InMemoryCreditLedger ledger = new();
    await ledger.CreditAsync(Principal, 1.00m, "seed-1");
    MockFacilitator facilitator = new();
    MeteredCapabilityGate gate = new(new PaymentGate(facilitator), ledger);

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      Principal,
      ReadyOptions(),
      paymentSignatureHeader: null);

    MeteredCapabilityGranted granted = outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    granted.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingCredit);
    granted.BalanceAfterDebit.ShouldBe(0.90m);
    granted.PaymentResponseHeader.ShouldBeNull();
    facilitator.VerifyCalls.ShouldBe(0);
    facilitator.SettleCalls.ShouldBe(0);
    (await ledger.GetBalanceAsync(Principal)).ShouldBe(0.90m);
  }

  public static async Task Unpaid_without_credit_returns_challenge()
  {
    InMemoryCreditLedger ledger = new();
    MeteredCapabilityGate gate = new(new PaymentGate(new MockFacilitator()), ledger);

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      PrincipalId.New(),
      ReadyOptions(),
      paymentSignatureHeader: null);

    MeteredCapabilityChallenge challenge = outcome.ShouldBeOfType<MeteredCapabilityChallenge>();
    challenge.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
    challenge.Payload.Accepts[0].Price.ShouldBe("$0.10");
  }

  public static async Task Valid_payment_settles_credits_then_debits()
  {
    PrincipalId principal = PrincipalId.New();
    InMemoryCreditLedger ledger = new();
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = "0xmetered-settle-1",
        Network = "eip155:84532",
        Payer = ValidPayTo,
      },
    };
    MeteredCapabilityGate gate = new(new PaymentGate(facilitator), ledger);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      principal,
      ReadyOptions(),
      signature);

    MeteredCapabilityGranted granted = outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    granted.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingPayment);
    granted.BalanceAfterDebit.ShouldBe(0m);
    granted.PaymentResponseHeader.ShouldNotBeNullOrWhiteSpace();
    facilitator.VerifyCalls.ShouldBe(1);
    facilitator.SettleCalls.ShouldBe(1);
    (await ledger.GetBalanceAsync(principal)).ShouldBe(0m);
  }

  public static async Task Disabled_payment_without_credit_returns_unavailable_not_challenge()
  {
    MeteredCapabilityGate gate = new(
      new PaymentGate(new MockFacilitator()),
      new InMemoryCreditLedger());

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      PrincipalId.New(),
      ReadyOptions() with { Enabled = false },
      paymentSignatureHeader: null);

    MeteredCapabilityUnavailable unavailable = outcome.ShouldBeOfType<MeteredCapabilityUnavailable>();
    unavailable.Status.ShouldBe(PaymentConfigStatus.Disabled);
    unavailable.ErrorCode.ShouldBe(PaymentConfigEvaluator.ErrorDisabled);
  }

  public static async Task Prepaid_credit_works_when_payment_disabled()
  {
    PrincipalId principal = PrincipalId.New();
    InMemoryCreditLedger ledger = new();
    await ledger.CreditAsync(principal, 0.25m, "seed-disabled");
    MeteredCapabilityGate gate = new(
      new PaymentGate(new MockFacilitator()),
      ledger);

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      principal,
      ReadyOptions() with { Enabled = false },
      paymentSignatureHeader: null);

    MeteredCapabilityGranted granted = outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    granted.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingCredit);
    granted.BalanceAfterDebit.ShouldBe(0.15m);
  }

  private static PaymentOptions ReadyOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/demo/metered-capability");
}

/// <summary>In-test facilitator (same shape as payment-gate-tests MockFacilitator).</summary>
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
