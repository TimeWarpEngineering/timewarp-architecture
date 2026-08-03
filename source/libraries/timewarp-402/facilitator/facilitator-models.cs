#region Purpose
// Facilitator verify/settle request and response shapes (mock- and HTTP-friendly).
#endregion

#region Design
// Grounded in @x402/core FacilitatorClient + tip.test.js mockFacilitator: isValid/invalidReason,
// success/errorReason/transaction/network/payer, kinds with x402Version/scheme/network.
// Payment payload and requirements travel as JsonElement so we do not invent rigid chain-specific
// schemas in the library; hosts and real buyers supply protocol-correct JSON.
#endregion

namespace TimeWarp.X402;

using System.Text.Json;
using System.Text.Json.Serialization;
/// <summary>POST body for facilitator <c>/verify</c> and <c>/settle</c>.</summary>
public sealed class FacilitatorPaymentRequest
{
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; } = 2;

  [JsonPropertyName("paymentPayload")]
  public required JsonElement PaymentPayload { get; init; }

  [JsonPropertyName("paymentRequirements")]
  public required JsonElement PaymentRequirements { get; init; }
}

/// <summary>Result of facilitator verification.</summary>
public sealed class FacilitatorVerifyResult
{
  [JsonPropertyName("isValid")]
  public required bool IsValid { get; init; }

  [JsonPropertyName("invalidReason")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? InvalidReason { get; init; }
}

/// <summary>Result of facilitator settlement.</summary>
public sealed class FacilitatorSettleResult
{
  [JsonPropertyName("success")]
  public required bool Success { get; init; }

  [JsonPropertyName("errorReason")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ErrorReason { get; init; }

  [JsonPropertyName("transaction")]
  public string Transaction { get; init; } = "";

  [JsonPropertyName("network")]
  public string Network { get; init; } = "";

  [JsonPropertyName("payer")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Payer { get; init; }
}

/// <summary>One supported payment kind from <c>getSupported</c>.</summary>
public sealed class FacilitatorKind
{
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; }

  [JsonPropertyName("scheme")]
  public required string Scheme { get; init; }

  [JsonPropertyName("network")]
  public required string Network { get; init; }
}

/// <summary>Facilitator <c>/supported</c> response (minimal fields we depend on).</summary>
public sealed class FacilitatorSupported
{
  [JsonPropertyName("kinds")]
  public IReadOnlyList<FacilitatorKind> Kinds { get; init; } = [];
}
