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
  [JsonPropertyName("ok")]
  public bool Ok { get; init; } = false;

  [JsonPropertyName("error")]
  public required string Error { get; init; }

  [JsonPropertyName("message")]
  public required string Message { get; init; }

  [JsonPropertyName("payment")]
  public bool Payment { get; init; } = true;
}
