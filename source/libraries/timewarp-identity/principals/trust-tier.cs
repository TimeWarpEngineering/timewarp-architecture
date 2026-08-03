#region Purpose
// Progressive authorization posture for a principal: identity is cheap; power is paid or earned.
#endregion

#region Design
// Progression-only ladder (RFC D1 C refined): None is reserved fail-closed zero; Provisional is birth floor
// (no credential yet); Keyed = has at least one credential; Funded = paid/settled path; Established = reputation.
// Quarantine is NOT a tier — it is an orthogonal bool on Principal (IsQuarantined). Free ordinal compares like
// tier >= Funded are forbidden in handlers; use named predicates (IsFundedAndActive). Transitions go through
// Promote / RecordCredentialAttached, not a free setter.
//
// Settlement composition (104-013, TimeWarp.X402.SettlementFundingService): successful x402 settle promotes
// to Funded when not quarantined. Debit / zero credit balance does NOT demote — Funded is "has settled at
// least once," orthogonal to current ledger balance. Immediate demotion after one metered use would reopen
// free Sybil tiers.
//
// Clocks (D5, closed 104-006): this enum has no timestamps — Principal/Credential CreatedAt/RevokedAt
// remain wall-clock with fuzzy tests; ceremony stores already use optional TimeProvider. Full
// TimeProvider on domain entities is not required for the Wave 1 gate.
#endregion

namespace TimeWarp.Identity;

public enum TrustTier
{
  None = 0,
  Provisional = 1,
  Keyed = 2,
  Funded = 3,
  Established = 4,
}
