#region Purpose
// Host-agnostic metered capability: debit prepaid credit, else x402 pay-then-debit for one use.
#endregion

#region Design
// Distinct from voluntary tip (104-009): this gate always bills a principal for a capability —
// payment-as-product, not a tip jar. Policy (locked product decision 8 + 104-010/011/013):
// 1. Parse options.Price → major units; fail → Unavailable misconfigured (503 never 402)
// 2. If balance >= price → DebitAsync → Granted(funding=credit) — works even when payment is
//    disabled (prepaid credit is already owned; free routes still never call this gate). Debit
//    does not demote TrustTier (Funded is "has settled", not "has balance" — see
//    SettlementFundingService).
// 3. Else PaymentGate.EvaluateAsync:
//    a. Unavailable → Unavailable (503)
//    b. Challenge / Rejected → pass through (402)
//    c. Settled → SettlementFundingService.ApplyAsync (credit + Funded promote) then DebitAsync
//       → Granted(funding=payment, PAYMENT-RESPONSE header)
// Credit-before-debit on settle keeps "ledger debit on every success" and reuses idempotent
// receipt application so facilitator retries do not double-fund. Partial balances are not
// combined with a partial payment in v1 — insufficient total balance goes fully through payment.
// FREE ROUTES NEVER call this type (host routing isolation).
//
// Lifetime: depends on SettlementFundingService → IPrincipalStore (scoped under EF). Register
// MeteredCapabilityGate as scoped on hosts that use EF principal stores.
#endregion

namespace TimeWarp.X402;

using TimeWarp.Identity;

/// <summary>Orchestrates prepaid credit debit and optional x402 pay-for-use for one capability.</summary>
public sealed class MeteredCapabilityGate
{
  private readonly PaymentGate PaymentGate;
  private readonly ICreditLedger Ledger;
  private readonly SettlementFundingService SettlementFunding;

  public MeteredCapabilityGate(
    PaymentGate paymentGate,
    ICreditLedger ledger,
    SettlementFundingService settlementFunding)
  {
    PaymentGate = paymentGate ?? throw new ArgumentNullException(nameof(paymentGate));
    Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    SettlementFunding = settlementFunding ?? throw new ArgumentNullException(nameof(settlementFunding));
  }

  /// <summary>
  /// Evaluates whether <paramref name="principalId"/> may invoke a metered capability priced by
  /// <paramref name="options"/>. Pass the raw <c>PAYMENT-SIGNATURE</c> header when present.
  /// </summary>
  public async Task<MeteredCapabilityOutcome> EvaluateAsync(
    PrincipalId principalId,
    PaymentOptions options,
    string? paymentSignatureHeader,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(options);

    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId must not be empty.", nameof(principalId));
    }

    if (!PaymentPrice.TryParseMajorUnits(options.Price, out decimal price))
    {
      return new MeteredCapabilityUnavailable(
        PaymentConfigStatus.Misconfigured,
        PaymentConfigEvaluator.ErrorMisconfigured,
        "Payment price is missing or not a positive dollar amount.");
    }

    decimal balance = await Ledger
      .GetBalanceAsync(principalId, cancellationToken)
      .ConfigureAwait(false);

    if (balance >= price)
    {
      decimal after = await Ledger
        .DebitAsync(principalId, price, operationId: null, cancellationToken)
        .ConfigureAwait(false);
      return new MeteredCapabilityGranted(
        after,
        MeteredCapabilityGranted.FundingCredit,
        PaymentResponseHeader: null);
    }

    PaymentGateOutcome paymentOutcome = await PaymentGate
      .EvaluateAsync(options, paymentSignatureHeader, cancellationToken)
      .ConfigureAwait(false);

    return paymentOutcome switch
    {
      PaymentUnavailable unavailable => new MeteredCapabilityUnavailable(
        unavailable.Status,
        unavailable.ErrorCode,
        unavailable.Message),
      PaymentChallenge challenge => new MeteredCapabilityChallenge(
        challenge.Payload,
        challenge.PaymentRequiredHeader),
      PaymentRejected rejected => new MeteredCapabilityRejected(
        rejected.Reason,
        rejected.Payload,
        rejected.PaymentRequiredHeader),
      PaymentSettled settled => await ApplySettlementThenDebitAsync(
        principalId,
        price,
        settled,
        cancellationToken).ConfigureAwait(false),
      _ => throw new InvalidOperationException(
        $"Unexpected payment gate outcome: {paymentOutcome.GetType().Name}"),
    };
  }

  private async Task<MeteredCapabilityGranted> ApplySettlementThenDebitAsync(
    PrincipalId principalId,
    decimal price,
    PaymentSettled settled,
    CancellationToken cancellationToken)
  {
    string receiptId = string.IsNullOrWhiteSpace(settled.Result.Transaction)
      ? $"settle:{Guid.NewGuid():N}"
      : settled.Result.Transaction;

    await SettlementFunding
      .ApplyAsync(principalId, price, receiptId, cancellationToken)
      .ConfigureAwait(false);

    decimal after = await Ledger
      .DebitAsync(principalId, price, operationId: $"metered:{receiptId}", cancellationToken)
      .ConfigureAwait(false);

    return new MeteredCapabilityGranted(
      after,
      MeteredCapabilityGranted.FundingPayment,
      settled.PaymentResponseHeader);
  }
}
