#region Purpose
// Tier for an SlaChip: good, warning, or behind.
#endregion

#region Design
// The chip does not score a percentage. The page passes the tier and a
// pre-formatted label. Good is the default, matching a status badge that
// defaults to the positive value. The accessible name leads with the tier
// because the fill is not the only signal.
#endregion

namespace TimeWarp.Architecture.Components;

public enum SlaTone
{
  Good,
  Warn,
  Bad,
}
