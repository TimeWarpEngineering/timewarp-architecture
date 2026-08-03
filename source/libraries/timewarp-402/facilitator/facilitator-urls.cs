#region Purpose
// Well-known facilitator base URLs (public constants only — no secrets).
#endregion

namespace TimeWarp.X402;

/// <summary>Documented facilitator endpoints. Secrets never live here.</summary>
public static class FacilitatorUrls
{
  /// <summary>Public testnet facilitator (Base Sepolia / Solana devnet style workflows).</summary>
  public const string X402Org = "https://x402.org/facilitator";

  /// <summary>Coinbase CDP hosted facilitator (requires auth headers from the host).</summary>
  public const string CdpPlatform = "https://api.cdp.coinbase.com/platform/v2/x402";
}
