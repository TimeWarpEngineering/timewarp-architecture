#region Purpose
// Host configuration for the voluntary x402 tip jar (binds to PaymentOptions for PaymentGate).
#endregion

#region Design
// Section name "TipOptions" (matches type name — AddFluentValidatedOptions binds by type name).
// Distinct from MeteredCapability (104-011): voluntary gratuity, no ledger debit, no agent auth.
// Enabled must be an explicit product decision — when false, PaymentGate returns 503 (never 402).
// PayTo is a public receive address only — never a merchant private key (tip-jar hard lesson).
// Env overlay (TIP_ENABLED, TIP_PAY_TO, TIP_NETWORK, TIP_PRICE, TIP_FACILITATOR_URL, TIP_ASSET)
// is applied in TipOptionsEnvironment so operators can match timewarp-software var names.
// Resource defaults to /api/tip — the canonical tip action URL (discovery alias /api is optional
// and left to 104-020).
#endregion

namespace TimeWarp.Architecture.Features.Tip.Application;

using TimeWarp.X402;

/// <summary>Configuration for the voluntary tip jar paid resource.</summary>
public class TipOptions
{
  public const string SectionName = "TipOptions";

  /// <summary>When false, tip path returns 503 (tips disabled — never 402).</summary>
  public bool Enabled { get; set; }

  /// <summary>EVM receive address (public). Never a private key.</summary>
  public string PayTo { get; set; } = "";

  /// <summary>CAIP-2 network id (default Base Sepolia for teachable local runs).</summary>
  public string Network { get; set; } = "eip155:84532";

  /// <summary>Dollar price string for exact scheme, e.g. <c>$0.10</c>.</summary>
  public string Price { get; set; } = "$0.10";

  /// <summary>Canonical resource path advertised in the payment challenge.</summary>
  public string Resource { get; set; } = "/api/tip";

  /// <summary>Facilitator base URL (x402.org testnet by default).</summary>
  public string FacilitatorBase { get; set; } = FacilitatorUrls.X402Org;

  /// <summary>Human-readable description for the accepts entry.</summary>
  public string Description { get; set; } =
    "Voluntary tip to TimeWarp Engineering. Content remains free; this is not a content fee.";

  /// <summary>Optional asset address (USDC); omitted when facilitator infers from network+price.</summary>
  public string? Asset { get; set; }

  /// <summary>When true, facilitator auth material must be present (mainnet CDP).</summary>
  public bool RequiresFacilitatorAuth { get; set; }

  /// <summary>Host reports facilitator auth material is configured (keys not stored here).</summary>
  public bool HasFacilitatorAuth { get; set; }

  public PaymentOptions ToPaymentOptions() =>
    new()
    {
      Enabled = Enabled,
      PayTo = PayTo,
      Network = Network,
      Price = Price,
      Resource = Resource,
      FacilitatorBase = FacilitatorBase,
      Description = Description,
      Asset = Asset,
      RequiresFacilitatorAuth = RequiresFacilitatorAuth,
      HasFacilitatorAuth = HasFacilitatorAuth,
      MimeType = "application/json",
    };
}
