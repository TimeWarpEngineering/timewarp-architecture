#region Purpose
// Port for x402 facilitator verify/settle/supported — swap x402.org, CDP, or in-test mocks.
#endregion

#region Design
// Mirrors @x402/core FacilitatorClient. The library never holds merchant private keys; facilitators
// verify signed buyer payloads and submit settlement. HTTP implementation uses plain URL + optional
// auth header factory (CDP JWT produced outside this package). Tests inject mocks (104-012).
#endregion

namespace TimeWarp.X402;

/// <summary>Facilitator verification and settlement client.</summary>
public interface IFacilitatorClient
{
  /// <summary>Lists schemes/networks the facilitator can handle.</summary>
  Task<FacilitatorSupported> GetSupportedAsync(CancellationToken cancellationToken = default);

  /// <summary>Verifies a payment payload against requirements without settling.</summary>
  Task<FacilitatorVerifyResult> VerifyAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Settles a verified payment on-chain (or facilitator-equivalent).</summary>
  Task<FacilitatorSettleResult> SettleAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default);
}
