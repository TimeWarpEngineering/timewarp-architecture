#region Purpose
// Surface tone for a compact IntegrationCard: default paper, or the warning wash.
#endregion

#region Design
// Warning paints only the compact row. Full keeps its paper surface and shows a
// warning through Status. There is no second sync-bar component.
#endregion

namespace TimeWarp.Architecture.Components;

public enum IntegrationCardTone
{
  Default,
  Warning,
}
