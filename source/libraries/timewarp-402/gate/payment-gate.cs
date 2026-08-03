#region Purpose
// Host-agnostic payment gate: disabled/misconfigured → unavailable; unpaid → challenge; paid → verify+settle.
#endregion

#region Design
// FREE ROUTES NEVER 402: this type is only invoked for resources the host has already classified
// as paid. Mounting it globally would reintroduce the tip-jar failure mode (free content returning
// 402). Hosts short-circuit free/discovery paths before calling EvaluateAsync.
//
// Policy order (locked product decision 8 + tip spike):
// 1. !Ready config → PaymentUnavailable (503) — never emit PAYMENT-REQUIRED
// 2. No / empty PAYMENT-SIGNATURE → PaymentChallenge (402)
// 3. Signature present → facilitator Verify then Settle
// 4. Invalid verify or failed settle → PaymentRejected (402 + challenge for retry)
// 5. Success → PaymentSettled (200 + PAYMENT-RESPONSE)
//
// Payment payload JSON is decoded from the signature header and passed through as JsonElement so
// the library stays chain-scheme agnostic. paymentRequirements is the first accepts entry from the
// challenge (exact scheme) as JSON.
#endregion

namespace TimeWarp.X402;

using System.Text.Json;
/// <summary>Orchestrates config policy, challenge building, and facilitator verify/settle.</summary>
public sealed class PaymentGate
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
  };

  private readonly IFacilitatorClient Facilitator;

  public PaymentGate(IFacilitatorClient facilitator)
  {
    Facilitator = facilitator ?? throw new ArgumentNullException(nameof(facilitator));
  }

  /// <summary>
  /// Evaluates a paid-resource request. Pass the raw <c>PAYMENT-SIGNATURE</c> header value when present.
  /// </summary>
  public async Task<PaymentGateOutcome> EvaluateAsync(
    PaymentOptions options,
    string? paymentSignatureHeader,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(options);

    PaymentConfigEvaluation config = PaymentConfigEvaluator.Evaluate(options);
    if (config.Status != PaymentConfigStatus.Ready)
    {
      return new PaymentUnavailable(
        config.Status,
        config.ErrorCode ?? PaymentConfigEvaluator.ErrorMisconfigured,
        config.Message ?? "Payment is unavailable.");
    }

    (PaymentRequiredPayload challengePayload, string challengeHeader) =
      PaymentChallengeBuilder.Build(options);

    if (string.IsNullOrWhiteSpace(paymentSignatureHeader))
    {
      return new PaymentChallenge(challengePayload, challengeHeader);
    }

    string? signatureJson = PaymentChallengeBuilder.TryDecodeHeaderPayload(paymentSignatureHeader);
    if (signatureJson is null)
    {
      return new PaymentRejected("malformed_payment_signature", challengePayload, challengeHeader);
    }

    JsonElement paymentPayloadElement;
    try
    {
      using var doc = JsonDocument.Parse(signatureJson);
      paymentPayloadElement = doc.RootElement.Clone();
    }
    catch (JsonException)
    {
      return new PaymentRejected("malformed_payment_signature_json", challengePayload, challengeHeader);
    }

    JsonElement requirementsElement = JsonSerializer.SerializeToElement(
      challengePayload.Accepts[0],
      JsonOptions);

    FacilitatorPaymentRequest facilitatorRequest = new()
    {
      X402Version = options.X402Version,
      PaymentPayload = paymentPayloadElement,
      PaymentRequirements = requirementsElement,
    };

    FacilitatorVerifyResult verify = await Facilitator
      .VerifyAsync(facilitatorRequest, cancellationToken)
      .ConfigureAwait(false);

    if (!verify.IsValid)
    {
      return new PaymentRejected(
        verify.InvalidReason ?? "invalid_payment",
        challengePayload,
        challengeHeader);
    }

    FacilitatorSettleResult settle = await Facilitator
      .SettleAsync(facilitatorRequest, cancellationToken)
      .ConfigureAwait(false);

    if (!settle.Success)
    {
      return new PaymentRejected(
        settle.ErrorReason ?? "settle_failed",
        challengePayload,
        challengeHeader);
    }

    string responseHeader = PaymentChallengeBuilder.EncodeHeaderPayload(settle);
    return new PaymentSettled(settle, responseHeader);
  }
}
