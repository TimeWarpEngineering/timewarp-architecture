#region Purpose
// Result of applying a successful x402 settlement to ledger credit and principal trust tier.
#endregion

#region Design
// Composition outcome (104-013): balance is always the post-credit ledger value (idempotent on
// receipt replay). PromotedToFunded is true only when this call advanced the principal to Funded;
// already-Funded/Established, quarantined, missing, or concurrent race that already funded → false.
// Callers that need the current tier re-read IPrincipalStore after ApplyAsync.
#endregion

namespace TimeWarp.X402;

/// <summary>Outcome of <see cref="SettlementFundingService.ApplyAsync"/>.</summary>
public sealed record SettlementFundingResult(
  decimal BalanceAfterCredit,
  bool PromotedToFunded);
