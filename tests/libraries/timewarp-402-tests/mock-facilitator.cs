#region Purpose
// CI-safe IFacilitatorClient double for package tests — never hits a live facilitator or chain.
#endregion

#region Design
// Shared across payment-gate, tip-path, and metered-gate suites so every wave-2 exit test uses the
// same mock shape (tip.test.js mockFacilitator: isValid/invalidReason, success/transaction).
// Mutable results allow per-test setup; Reset() clears call counters between cases in shared fixtures.
#endregion

namespace TimeWarp.X402.TestSupport;

/// <summary>In-test facilitator matching the tip/spike mockFacilitator shape (no network).</summary>
internal sealed class MockFacilitator : IFacilitatorClient
{
  public FacilitatorVerifyResult VerifyResult { get; set; } =
    new() { IsValid = false, InvalidReason = "invalid_payload" };

  public FacilitatorSettleResult SettleResult { get; set; } =
    new() { Success = false, ErrorReason = "not_implemented", Network = "eip155:84532" };

  public int VerifyCalls { get; private set; }
  public int SettleCalls { get; private set; }
  public int SupportedCalls { get; private set; }

  public FacilitatorPaymentRequest? LastVerifyRequest { get; private set; }
  public FacilitatorPaymentRequest? LastSettleRequest { get; private set; }

  public void Reset()
  {
    VerifyCalls = 0;
    SettleCalls = 0;
    SupportedCalls = 0;
    LastVerifyRequest = null;
    LastSettleRequest = null;
  }

  public Task<FacilitatorSupported> GetSupportedAsync(CancellationToken cancellationToken = default)
  {
    SupportedCalls++;
    return Task.FromResult(new FacilitatorSupported
    {
      Kinds =
      [
        new FacilitatorKind { X402Version = 2, Scheme = "exact", Network = "eip155:84532" },
      ],
    });
  }

  public Task<FacilitatorVerifyResult> VerifyAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    VerifyCalls++;
    LastVerifyRequest = request;
    return Task.FromResult(VerifyResult);
  }

  public Task<FacilitatorSettleResult> SettleAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    SettleCalls++;
    LastSettleRequest = request;
    return Task.FromResult(SettleResult);
  }
}
