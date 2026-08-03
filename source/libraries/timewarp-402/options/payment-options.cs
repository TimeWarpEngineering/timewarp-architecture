#region Purpose
// Seller-side configuration for a single paid resource (tip, meter, or other capability).
#endregion

#region Design
// Host-agnostic: env mapping (TIP_*, PAY_*, etc.) stays at the host (104-009+). This type is the
// library's normalized view after the host has resolved flags and secrets.
// Enabled must be an explicit product decision — hosts should set Enabled only when the flag is
// the string "true" (tip-jar hard lesson: "1" / "false" / missing must not look enabled).
// FacilitatorUrl points at x402.org (testnet) or CDP-shaped production; auth headers are supplied
// by the IFacilitatorClient implementation, not stored as private keys on this options object.
// RequiresFacilitatorAuth + HasFacilitatorAuth model mainnet CDP: when the network is production
// and the host requires authenticated facilitator, missing auth is misconfiguration → 503, not 402.
#endregion

namespace TimeWarp.X402;

/// <summary>Configuration for one paid HTTP resource.</summary>
public sealed record PaymentOptions
{
  /// <summary>When false, the gate returns 503-class disabled (never 402).</summary>
  public required bool Enabled { get; init; }

  /// <summary>EVM receive address (public). Never a private key.</summary>
  public required string PayTo { get; init; }

  /// <summary>CAIP-2 network id, e.g. <c>eip155:84532</c> (Base Sepolia) or <c>eip155:8453</c>.</summary>
  public required string Network { get; init; }

  /// <summary>Dollar price string for exact scheme, e.g. <c>$0.10</c>.</summary>
  public required string Price { get; init; }

  /// <summary>Canonical resource path/URL advertised in the challenge (aliases should normalize here).</summary>
  public required string Resource { get; init; }

  /// <summary>Facilitator base address (no trailing slash required). Wire string, not System.Uri.</summary>
  public required string FacilitatorBase { get; init; }

  /// <summary>Optional asset address (e.g. USDC). May be omitted when the facilitator infers from network+price.</summary>
  public string? Asset { get; init; }

  /// <summary>Human-readable description for the accepts entry.</summary>
  public string Description { get; init; } = "Paid resource";

  /// <summary>Payment scheme (v1 surface: <c>exact</c> only).</summary>
  public string Scheme { get; init; } = "exact";

  /// <summary>x402 protocol version advertised in challenges.</summary>
  public int X402Version { get; init; } = 2;

  /// <summary>
  /// When true, a valid facilitator auth configuration is required (e.g. mainnet CDP).
  /// Missing auth → misconfigured → 503, never 402.
  /// </summary>
  public bool RequiresFacilitatorAuth { get; init; }

  /// <summary>Host reports that facilitator auth material is present (keys not stored here).</summary>
  public bool HasFacilitatorAuth { get; init; }

  /// <summary>MIME type of the successful response body (informational for buyers).</summary>
  public string MimeType { get; init; } = "application/json";

  public static PaymentOptions CreateTestnetDefaults(
    string payTo,
    string resource,
    string price = "$0.10",
    string network = "eip155:84532") =>
    new()
    {
      Enabled = true,
      PayTo = payTo,
      Network = network,
      Price = price,
      Resource = resource,
      FacilitatorBase = FacilitatorUrls.X402Org,
      RequiresFacilitatorAuth = false,
      HasFacilitatorAuth = false,
    };
}
