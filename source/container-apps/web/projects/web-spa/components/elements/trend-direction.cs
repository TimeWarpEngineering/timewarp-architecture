#region Purpose
// Direction for a TrendPill: up, down, or flat.
#endregion

#region Design
// The pill does not compute a delta. The page passes the direction and a
// pre-formatted label. Flat is the default so an unset pill does not claim a rise.
#endregion

namespace TimeWarp.Architecture.Components;

public enum TrendDirection
{
  Flat,
  Up,
  Down,
}
