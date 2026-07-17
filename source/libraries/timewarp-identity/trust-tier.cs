#region Purpose
// Progressive authorization posture for a principal: identity is cheap; power is paid or earned.
#endregion

#region Design
// Progression-only ladder (RFC D1 C refined): None is reserved fail-closed zero; Provisional is birth floor
// (no credential yet); Keyed = has at least one credential; Funded = paid/credit path; Established = reputation.
// Quarantine is NOT a tier — it is an orthogonal bool on Principal (IsQuarantined). Free ordinal compares like
// tier >= Funded are forbidden in handlers; use named predicates (IsFundedAndActive). Transitions go through
// Promote / RecordCredentialAttached, not a free setter.
// D5: TimeProvider for CreatedAt/RevokedAt is deferred to 104-006.
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
