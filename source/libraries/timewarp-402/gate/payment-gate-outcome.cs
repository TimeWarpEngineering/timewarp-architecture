#region Purpose
// Discriminated outcomes of the payment gate for host HTTP mapping (no ASP.NET dependency).
#endregion

#region Design
// Hosts map:
//   Unavailable → 503 + PaymentErrorPayload (never PAYMENT-REQUIRED)
//   Challenge   → 402 + PAYMENT-REQUIRED header
//   Rejected    → 402 + PAYMENT-REQUIRED (retry) + optional invalid reason
//   Settled     → 200 + PAYMENT-RESPONSE + host business body
// Free routes never invoke the gate — isolation is a host routing concern (tip-jar hard lesson).
#endregion

namespace TimeWarp.X402;

/// <summary>Result of evaluating a paid-resource request through <see cref="PaymentGate"/>.</summary>
public abstract record PaymentGateOutcome;

/// <summary>Payment feature off or misconfigured — host must respond 503, never 402.</summary>
public sealed record PaymentUnavailable(
  PaymentConfigStatus Status,
  string ErrorCode,
  string Message) : PaymentGateOutcome
{
  public PaymentErrorPayload ToErrorPayload() => new()
  {
    Error = ErrorCode,
    Message = Message,
  };
}

/// <summary>Configured unpaid request — host responds 402 with the challenge header.</summary>
public sealed record PaymentChallenge(
  PaymentRequiredPayload Payload,
  string PaymentRequiredHeader) : PaymentGateOutcome;

/// <summary>Payment presented but verify/settle failed — host responds 402 with a fresh challenge.</summary>
public sealed record PaymentRejected(
  string? Reason,
  PaymentRequiredPayload Payload,
  string PaymentRequiredHeader) : PaymentGateOutcome;

/// <summary>Payment verified and settled — host responds 200 with business body + response header.</summary>
public sealed record PaymentSettled(
  FacilitatorSettleResult Result,
  string PaymentResponseHeader) : PaymentGateOutcome;
