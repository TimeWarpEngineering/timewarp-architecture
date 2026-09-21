#region Purpose
// Structured JSON body for 503 payment-disabled / misconfigured responses (never 402).
#endregion

namespace TimeWarp.X402;

using System.Text.Json.Serialization;
/// <summary>
/// Host maps this to HTTP 503. Includes a <c>payment</c> marker so clients can distinguish
/// payment-surface errors from generic service errors (tip jar used <c>tip: true</c>).
/// </summary>
public sealed class PaymentErrorPayload
{
  /// <summary>Always false for payment-surface error responses.</summary>
  [JsonPropertyName("ok")]
  public bool Ok { get; init; } = false;

  /// <summary>Machine-readable error code such as <c>payment_disabled</c> or <c>payment_misconfigured</c>.</summary>
  [JsonPropertyName("error")]
  public required string Error { get; init; }

  /// <summary>Human-readable explanation of why payment is unavailable.</summary>
  [JsonPropertyName("message")]
  public required string Message { get; init; }

  /// <summary>Marks this 503 as a payment-surface error rather than a generic outage.</summary>
  [JsonPropertyName("payment")]
  public bool Payment { get; init; } = true;
}
