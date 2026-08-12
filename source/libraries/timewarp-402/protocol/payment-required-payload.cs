#region Purpose
// JSON shape encoded into the PAYMENT-REQUIRED header (x402 v2).
#endregion

namespace TimeWarp.X402;

using System.Text.Json.Serialization;
/// <summary>Payment requirements payload buyers decode from <see cref="PaymentHeaders.PaymentRequired"/>.</summary>
public sealed class PaymentRequiredPayload
{
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; } = 2;

  [JsonPropertyName("accepts")]
  public required IReadOnlyList<PaymentAccept> Accepts { get; init; }

  [JsonPropertyName("resource")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public PaymentResource? Resource { get; init; }
}

/// <summary>Optional resource metadata inside a payment-required payload.</summary>
public sealed class PaymentResource
{
  [JsonPropertyName("url")]
  public required string Path { get; init; }

  [JsonPropertyName("description")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Description { get; init; }
}
