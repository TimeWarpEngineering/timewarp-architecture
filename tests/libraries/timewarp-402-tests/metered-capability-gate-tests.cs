#region Purpose
// MeteredCapabilityGate: credit debit, 402 challenge/reject, settle→fund→debit, disabled→503.
#endregion

#region Design
// Host maps Granted→200, Challenge/Rejected→402, Unavailable→503. Free routes never call this gate.
// Mock facilitator only — no live chain (104-012 package exit). SettlementFundingService uses an
// empty principal store in most cases (credit still applies); tier-focused cases seed a Keyed agent.
#endregion

namespace MeteredCapabilityGate_;

using TimeWarp.Identity;
using TimeWarp.X402;
using TimeWarp.X402.TestSupport;

public class EvaluateAsync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EvaluateAsync>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";
  private static readonly PrincipalId PrepaidPrincipalId = PrincipalId.New();

  public static async Task Prepaid_credit_debits_without_facilitator()
  {
    InMemoryCreditLedger ledger = new();
    await ledger.CreditAsync(PrepaidPrincipalId, 1.00m, "seed-1");
    MockFacilitator facilitator = new();
    MeteredCapabilityGate gate = CreateGate(facilitator, ledger);

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      PrepaidPrincipalId,
      ReadyOptions(),
      paymentSignatureHeader: null);

    MeteredCapabilityGranted granted = outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    granted.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingCredit);
    granted.BalanceAfterDebit.ShouldBe(0.90m);
    granted.PaymentResponseHeader.ShouldBeNull();
    facilitator.VerifyCalls.ShouldBe(0);
    facilitator.SettleCalls.ShouldBe(0);
    (await ledger.GetBalanceAsync(PrepaidPrincipalId)).ShouldBe(0.90m);
  }

  public static async Task Unpaid_without_credit_returns_challenge()
  {
    InMemoryCreditLedger ledger = new();
    MeteredCapabilityGate gate = CreateGate(new MockFacilitator(), ledger);

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
    MeteredCapabilityGate gate = CreateGate(facilitator, ledger);
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

  public static async Task Valid_payment_promotes_principal_to_funded_and_debit_keeps_tier()
  {
    InMemoryPrincipalStore store = new();
    Principal agent = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(agent);
    Credential credential = Credential.Create(
      agent.Id,
      CredentialType.AgentKey,
      handle: Guid.NewGuid().ToByteArray(),
      publicMaterial: Guid.NewGuid().ToByteArray());
    await store.AddCredentialAsync(credential);
    (await store.GetPrincipalAsync(agent.Id))!.TrustTier.ShouldBe(TrustTier.Keyed);

    InMemoryCreditLedger ledger = new();
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = "0xmetered-fund-tier",
        Network = "eip155:84532",
        Payer = ValidPayTo,
      },
    };
    MeteredCapabilityGate gate = CreateGate(facilitator, ledger, store);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(agent.Id, ReadyOptions(), signature);

    outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    (await ledger.GetBalanceAsync(agent.Id)).ShouldBe(0m);
    Principal? after = await store.GetPrincipalAsync(agent.Id);
    after.ShouldNotBeNull();
    after.TrustTier.ShouldBe(TrustTier.Funded);
    after.IsFundedAndActive.ShouldBeTrue();
  }

  public static async Task Invalid_payment_returns_rejected_without_ledger_change()
  {
    PrincipalId principal = PrincipalId.New();
    InMemoryCreditLedger ledger = new();
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = false, InvalidReason = "bad_sig" },
    };
    MeteredCapabilityGate gate = CreateGate(facilitator, ledger);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2 });

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(principal, ReadyOptions(), signature);

    MeteredCapabilityRejected rejected = outcome.ShouldBeOfType<MeteredCapabilityRejected>();
    rejected.Reason.ShouldBe("bad_sig");
    rejected.PaymentRequiredHeader.ShouldNotBeNullOrWhiteSpace();
    facilitator.SettleCalls.ShouldBe(0);
    (await ledger.GetBalanceAsync(principal)).ShouldBe(0m);
  }

  public static async Task Disabled_payment_without_credit_returns_unavailable_not_challenge()
  {
    MeteredCapabilityGate gate = CreateGate(new MockFacilitator(), new InMemoryCreditLedger());

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
    MeteredCapabilityGate gate = CreateGate(new MockFacilitator(), ledger);

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      principal,
      ReadyOptions() with { Enabled = false },
      paymentSignatureHeader: null);

    MeteredCapabilityGranted granted = outcome.ShouldBeOfType<MeteredCapabilityGranted>();
    granted.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingCredit);
    granted.BalanceAfterDebit.ShouldBe(0.15m);
  }

  public static async Task Invalid_price_returns_unavailable_not_challenge()
  {
    MeteredCapabilityGate gate = CreateGate(new MockFacilitator(), new InMemoryCreditLedger());

    MeteredCapabilityOutcome outcome = await gate.EvaluateAsync(
      PrincipalId.New(),
      ReadyOptions() with { Price = "not-a-price" },
      paymentSignatureHeader: null);

    MeteredCapabilityUnavailable unavailable = outcome.ShouldBeOfType<MeteredCapabilityUnavailable>();
    unavailable.Status.ShouldBe(PaymentConfigStatus.Misconfigured);
    unavailable.ErrorCode.ShouldBe(PaymentConfigEvaluator.ErrorMisconfigured);
  }

  public static async Task Settle_receipt_is_idempotent_on_retry()
  {
    PrincipalId principal = PrincipalId.New();
    InMemoryCreditLedger ledger = new();
    const string Tx = "0xmetered-idempotent-tx";
    MockFacilitator facilitator = new()
    {
      VerifyResult = new FacilitatorVerifyResult { IsValid = true },
      SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = Tx,
        Network = "eip155:84532",
        Payer = ValidPayTo,
      },
    };
    MeteredCapabilityGate gate = CreateGate(facilitator, ledger);
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });

    // First invoke: credit by receipt then debit → balance 0.
    (await gate.EvaluateAsync(principal, ReadyOptions(), signature))
      .ShouldBeOfType<MeteredCapabilityGranted>();

    // Seed extra credit and re-apply same receipt id via ledger (simulates settle retry credit).
    await ledger.CreditAsync(principal, 0.10m, Tx);
    (await ledger.GetBalanceAsync(principal)).ShouldBe(0m);
  }

  private static MeteredCapabilityGate CreateGate(
    IFacilitatorClient facilitator,
    ICreditLedger ledger,
    IPrincipalStore? principals = null)
  {
    SettlementFundingService funding = new(ledger, principals ?? new InMemoryPrincipalStore());
    return new MeteredCapabilityGate(new PaymentGate(facilitator), ledger, funding);
  }

  private static PaymentOptions ReadyOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/demo/metered-capability");
}
