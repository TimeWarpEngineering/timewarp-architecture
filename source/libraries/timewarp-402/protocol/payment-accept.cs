#region Purpose
// One accepted payment method advertised inside a PAYMENT-REQUIRED challenge.
#endregion

namespace TimeWarp.X402;

using System.Text.Json.Serialization;
/// <summary>Single entry in the x402 <c>accepts</c> array (exact scheme for v1).</summary>
public sealed class PaymentAccept
{
  [JsonPropertyName("scheme")]
  public required string Scheme { get; init; }

  [JsonPropertyName("network")]
  public required string Network { get; init; }

  [JsonPropertyName("payTo")]
  public required string PayTo { get; init; }

  /// <summary>Dollar string price (e.g. <c>$0.10</c>) or facilitator-accepted price form.</summary>
  [JsonPropertyName("price")]
  public required string Price { get; init; }

  [JsonPropertyName("description")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Description { get; init; }

  [JsonPropertyName("mimeType")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? MimeType { get; init; }

  [JsonPropertyName("asset")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Asset { get; init; }

  [JsonPropertyName("maxTimeoutSeconds")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MaxTimeoutSeconds { get; init; }
}
