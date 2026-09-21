#region Purpose
// JSON shape encoded into the PAYMENT-REQUIRED header (x402 v2).
#endregion

namespace TimeWarp.X402;

using System.Text.Json.Serialization;
/// <summary>Payment requirements payload buyers decode from <see cref="PaymentHeaders.PaymentRequired"/>.</summary>
public sealed class PaymentRequiredPayload
{
  /// <summary>x402 protocol version advertised in the challenge.</summary>
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; } = 2;

  /// <summary>Payment methods the seller accepts for this resource.</summary>
  [JsonPropertyName("accepts")]
  public required IReadOnlyList<PaymentAccept> Accepts { get; init; }

  /// <summary>Optional resource URL and description attached to the challenge.</summary>
  [JsonPropertyName("resource")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public PaymentResource? Resource { get; init; }
}

/// <summary>Optional resource metadata inside a payment-required payload.</summary>
public sealed class PaymentResource
{
  /// <summary>Canonical resource path or URL being paid for.</summary>
  [JsonPropertyName("url")]
  public required string Path { get; init; }

  /// <summary>Optional human-readable description of the resource.</summary>
  [JsonPropertyName("description")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Description { get; init; }
}
