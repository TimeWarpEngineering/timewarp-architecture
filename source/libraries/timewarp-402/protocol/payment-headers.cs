#region Purpose
// Canonical x402 v2 HTTP header names for buyer/seller interop (@x402/fetch).
#endregion

namespace TimeWarp.X402;

/// <summary>HTTP header names used by the x402 payment protocol (v2).</summary>
public static class PaymentHeaders
{
  /// <summary>Server → client: Base64-encoded payment requirements JSON.</summary>
  public const string PaymentRequired = "PAYMENT-REQUIRED";

  /// <summary>Client → server: Base64-encoded signed payment payload JSON.</summary>
  public const string PaymentSignature = "PAYMENT-SIGNATURE";

  /// <summary>Server → client: Base64-encoded settlement result JSON.</summary>
  public const string PaymentResponse = "PAYMENT-RESPONSE";
}
