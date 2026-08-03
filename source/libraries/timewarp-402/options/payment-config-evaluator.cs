#region Purpose
// Classifies payment options into ready / disabled / misconfigured before any 402 is emitted.
#endregion

#region Design
// Free routes never call this. Paid-path hosts call Evaluate first: only Ready may produce
// PAYMENT-REQUIRED. Disabled and Misconfigured map to HTTP 503 with structured error codes so
// agents/scanners never interpret a dark payment surface as "pay here" (402).
#endregion

namespace TimeWarp.X402;

/// <summary>Outcome of validating <see cref="PaymentOptions"/> without performing a challenge.</summary>
public enum PaymentConfigStatus
{
  /// <summary>Feature off — map to 503 payment_disabled.</summary>
  Disabled = 0,

  /// <summary>Feature on but config incomplete/invalid — map to 503 payment_misconfigured.</summary>
  Misconfigured = 1,

  /// <summary>Safe to build 402 challenges and attempt verify/settle.</summary>
  Ready = 2,
}

/// <summary>Result of <see cref="PaymentConfigEvaluator.Evaluate"/>.</summary>
public sealed record PaymentConfigEvaluation(
  PaymentConfigStatus Status,
  string? ErrorCode,
  string? Message);

/// <summary>Validates seller options before challenge emission.</summary>
public static class PaymentConfigEvaluator
{
  public const string ErrorDisabled = "payment_disabled";
  public const string ErrorMisconfigured = "payment_misconfigured";

  public static PaymentConfigEvaluation Evaluate(PaymentOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    if (!options.Enabled)
    {
      return new(
        PaymentConfigStatus.Disabled,
        ErrorDisabled,
        "Payment for this resource is disabled.");
    }

    if (!PayToValidator.IsValid(options.PayTo))
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Payment receive address (payTo) is missing or invalid.");
    }

    if (string.IsNullOrWhiteSpace(options.Network))
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Payment network is missing.");
    }

    if (string.IsNullOrWhiteSpace(options.Price))
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Payment price is missing.");
    }

    if (string.IsNullOrWhiteSpace(options.Resource))
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Payment resource URL is missing.");
    }

    if (string.IsNullOrWhiteSpace(options.FacilitatorBase))
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Facilitator URL is missing.");
    }

    if (options.RequiresFacilitatorAuth && !options.HasFacilitatorAuth)
    {
      return new(
        PaymentConfigStatus.Misconfigured,
        ErrorMisconfigured,
        "Facilitator authentication is required but not configured.");
    }

    return new(PaymentConfigStatus.Ready, null, null);
  }
}
