#region Purpose
// Discriminated outcomes of metered capability evaluation (credit debit and/or x402 payment).
#endregion

#region Design
// Mirrors PaymentGateOutcome for the payment branch; Granted is the metered success path after a
// ledger debit (and optional settle credit). Hosts map:
//   Unavailable → 503 (never 402)
//   Challenge / Rejected → 402 + PAYMENT-REQUIRED
//   Granted → 200 + business body (+ PAYMENT-RESPONSE when settlement funded the debit)
// Free routes never invoke MeteredCapabilityGate.
#endregion

namespace TimeWarp.X402;

/// <summary>Result of evaluating a metered capability request.</summary>
public abstract record MeteredCapabilityOutcome;

/// <summary>Payment feature off or misconfigured — host responds 503, never 402.</summary>
public sealed record MeteredCapabilityUnavailable(
  PaymentConfigStatus Status,
  string ErrorCode,
  string Message) : MeteredCapabilityOutcome
{
  public PaymentErrorPayload ToErrorPayload() => new()
  {
    Error = ErrorCode,
    Message = Message,
  };
}

/// <summary>Insufficient credit and unpaid — host responds 402 with the challenge header.</summary>
public sealed record MeteredCapabilityChallenge(
  PaymentRequiredPayload Payload,
  string PaymentRequiredHeader) : MeteredCapabilityOutcome;

/// <summary>Payment presented but verify/settle failed — host responds 402 with a fresh challenge.</summary>
public sealed record MeteredCapabilityRejected(
  string? Reason,
  PaymentRequiredPayload Payload,
  string PaymentRequiredHeader) : MeteredCapabilityOutcome;

/// <summary>
/// Capability authorized: ledger debited. <see cref="PaymentResponseHeader"/> is set when an
/// on-request payment settlement funded the balance; null when prepaid credit covered the debit.
/// </summary>
public sealed record MeteredCapabilityGranted(
  decimal BalanceAfterDebit,
  string FundingSource,
  string? PaymentResponseHeader) : MeteredCapabilityOutcome
{
  /// <summary>Prepaid ledger balance covered the price (no facilitator call).</summary>
  public const string FundingCredit = "credit";

  /// <summary>On-request x402 settle credited then debited the price.</summary>
  public const string FundingPayment = "payment";
}
