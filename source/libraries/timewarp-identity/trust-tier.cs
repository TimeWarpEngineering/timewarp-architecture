#region Purpose
// Progressive authorization posture for a principal: identity is cheap; power is paid or earned.
#endregion

#region Design
// Keyed = has at least one credential (can authenticate). Funded = paid/credit path has succeeded (agent power).
// Established and Quarantined are later operational tiers (reputation / risk). Cheap to get an id; expensive to act.
#endregion

namespace TimeWarp.Identity;

public enum TrustTier
{
  Keyed = 0,
  Funded = 1,
  Established = 2,
  Quarantined = 3,
}
