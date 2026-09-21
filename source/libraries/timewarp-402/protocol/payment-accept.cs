#region Purpose
// One accepted payment method advertised inside a PAYMENT-REQUIRED challenge.
#endregion

namespace TimeWarp.X402;

using System.Text.Json.Serialization;
/// <summary>Single entry in the x402 <c>accepts</c> array (exact scheme for v1).</summary>
public sealed class PaymentAccept
{
  /// <summary>Payment scheme identifier (<c>exact</c> for v1).</summary>
  [JsonPropertyName("scheme")]
  public required string Scheme { get; init; }

  /// <summary>CAIP-2 network id the seller accepts payment on.</summary>
  [JsonPropertyName("network")]
  public required string Network { get; init; }

  /// <summary>EVM receive address for the payment (public; never a private key).</summary>
  [JsonPropertyName("payTo")]
  public required string PayTo { get; init; }

  /// <summary>Dollar string price (e.g. <c>$0.10</c>) or facilitator-accepted price form.</summary>
  [JsonPropertyName("price")]
  public required string Price { get; init; }

  /// <summary>Human-readable description of what the payment covers.</summary>
  [JsonPropertyName("description")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Description { get; init; }

  /// <summary>MIME type of the successful paid response body.</summary>
  [JsonPropertyName("mimeType")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? MimeType { get; init; }

  /// <summary>Optional asset contract address (for example USDC); omitted when inferred from network and price.</summary>
  [JsonPropertyName("asset")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Asset { get; init; }

  /// <summary>Optional buyer timeout hint in seconds for completing payment.</summary>
  [JsonPropertyName("maxTimeoutSeconds")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MaxTimeoutSeconds { get; init; }
}
