#region Purpose
// TimeWarp.402 demo services: the in-memory credit ledger, the facilitator client, and the payment gates.
#endregion

#region Design
// Free and discovery routes never resolve these gates; only the tip and metered handlers do.
// SettlementFundingService and MeteredCapabilityGate are scoped because postgres swaps
// IPrincipalStore to the scoped EfPrincipalStore (no captive dependency).
#endregion

namespace TimeWarp.Architecture.Web.Server;

/// <summary>Registers the x402 tip and metered-capability demo services.</summary>
internal static class WebServerPaymentServices
{
  /// <summary>Ledger, facilitator, payment gate, settlement funding and metered gate.</summary>
  internal static void ConfigureServices(IServiceCollection serviceCollection)
  {
    // TimeWarp.402 demos (104-009 tip, 104-011 metered, 104-013 settle→Funded):
    // in-memory ledger + facilitator + gates. Free/discovery routes never resolve PaymentGate /
    // MeteredCapabilityGate / SettlementFundingService — only tip and metered handlers invoke them.
    // Facilitator base prefers tip options, then metered, then public testnet facilitator.
    // SettlementFundingService + MeteredCapabilityGate are scoped so they can resolve IPrincipalStore
    // when postgres swaps the store to scoped EfPrincipalStore (captive-dependency safe).
    serviceCollection.AddSingleton<ICreditLedger, InMemoryCreditLedger>();
    serviceCollection.AddSingleton<IFacilitatorClient>(static serviceProvider =>
    {
      TipOptions tip = serviceProvider.GetRequiredService<IOptions<TipOptions>>().Value;
      MeteredCapabilityOptions metered = serviceProvider
        .GetRequiredService<IOptions<MeteredCapabilityOptions>>()
        .Value;
      string facilitatorBase =
        !string.IsNullOrWhiteSpace(tip.FacilitatorBase) ? tip.FacilitatorBase
        : !string.IsNullOrWhiteSpace(metered.FacilitatorBase) ? metered.FacilitatorBase
        : FacilitatorUrls.X402Org;
      return new HttpFacilitatorClient(facilitatorBase);
    });
    serviceCollection.AddSingleton<PaymentGate>();
    serviceCollection.AddScoped<SettlementFundingService>();
    serviceCollection.AddScoped<MeteredCapabilityGate>();
  }
}
