#region Purpose
// Host configuration for the metered pay-for-capability demo (binds to PaymentOptions for the gate).
#endregion

#region Design
// Section name "MeteredCapabilityOptions" (matches type name — AddFluentValidatedOptions binds by
// type name). Lives in web-application so the handler and validator share it with web-server's
// registration. Enabled must be the product decision for the paid surface — when false,
// MeteredCapabilityGate's payment branch returns 503 (never 402); prepaid credit can still debit
// when balance is sufficient. PayTo is a public receive address only — never a merchant private key.
// Resource defaults to the InvokeMeteredCapability route so challenge aliases normalize here.
#endregion

namespace TimeWarp.Architecture.Features.MeteredCapability.Application;

using TimeWarp.X402;

/// <summary>Configuration for the metered capability demo paid resource.</summary>
public class MeteredCapabilityOptions
{
  public const string SectionName = "MeteredCapabilityOptions";

  /// <summary>When false, unpaid/insufficient-credit path returns 503 (payment disabled).</summary>
  public bool Enabled { get; set; }

  /// <summary>EVM receive address (public). Never a private key.</summary>
  public string PayTo { get; set; } = "";

  /// <summary>CAIP-2 network id (default Base Sepolia).</summary>
  public string Network { get; set; } = "eip155:84532";

  /// <summary>Dollar price string for exact scheme, e.g. <c>$0.10</c>.</summary>
  public string Price { get; set; } = "$0.10";

  /// <summary>Canonical resource path advertised in the payment challenge.</summary>
  public string Resource { get; set; } = "/api/demo/metered-capability";

  /// <summary>Facilitator base URL (x402.org testnet by default).</summary>
  public string FacilitatorBase { get; set; } = FacilitatorUrls.X402Org;

  /// <summary>Human-readable description for the accepts entry.</summary>
  public string Description { get; set; } = "Metered expensive capability demo";

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
