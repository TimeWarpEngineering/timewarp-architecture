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
  /// <summary>x402 protocol version sent with the facilitator request.</summary>
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; } = 2;

  /// <summary>Buyer payment proof JSON decoded from the <c>PAYMENT-SIGNATURE</c> header.</summary>
  [JsonPropertyName("paymentPayload")]
  public required JsonElement PaymentPayload { get; init; }

  /// <summary>Payment requirements JSON the proof must satisfy (typically the first accepts entry).</summary>
  [JsonPropertyName("paymentRequirements")]
  public required JsonElement PaymentRequirements { get; init; }
}

/// <summary>Result of facilitator verification.</summary>
public sealed class FacilitatorVerifyResult
{
  /// <summary>Whether the facilitator accepted the payment payload against the requirements.</summary>
  [JsonPropertyName("isValid")]
  public required bool IsValid { get; init; }

  /// <summary>Machine-readable reason when verification fails; null when valid.</summary>
  [JsonPropertyName("invalidReason")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? InvalidReason { get; init; }
}

/// <summary>Result of facilitator settlement.</summary>
public sealed class FacilitatorSettleResult
{
  /// <summary>Whether settlement completed successfully.</summary>
  [JsonPropertyName("success")]
  public required bool Success { get; init; }

  /// <summary>Machine-readable settle failure reason; null when successful.</summary>
  [JsonPropertyName("errorReason")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ErrorReason { get; init; }

  /// <summary>Settlement transaction id or hash used as a ledger receipt.</summary>
  [JsonPropertyName("transaction")]
  public string Transaction { get; init; } = "";

  /// <summary>Network on which settlement occurred (CAIP-2 style when provided).</summary>
  [JsonPropertyName("network")]
  public string Network { get; init; } = "";

  /// <summary>Payer address reported by the facilitator when available.</summary>
  [JsonPropertyName("payer")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Payer { get; init; }
}

/// <summary>One supported payment kind from <c>getSupported</c>.</summary>
public sealed class FacilitatorKind
{
  /// <summary>x402 protocol version this kind supports.</summary>
  [JsonPropertyName("x402Version")]
  public int X402Version { get; init; }

  /// <summary>Payment scheme identifier (for example <c>exact</c>).</summary>
  [JsonPropertyName("scheme")]
  public required string Scheme { get; init; }

  /// <summary>CAIP-2 network id this kind supports.</summary>
  [JsonPropertyName("network")]
  public required string Network { get; init; }
}

/// <summary>Facilitator <c>/supported</c> response (minimal fields we depend on).</summary>
public sealed class FacilitatorSupported
{
  /// <summary>Scheme and network combinations the facilitator can verify and settle.</summary>
  [JsonPropertyName("kinds")]
  public IReadOnlyList<FacilitatorKind> Kinds { get; init; } = [];
}
