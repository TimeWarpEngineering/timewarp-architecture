#region Purpose
// Border accent for a KpiCard: none, positive, or danger.
#endregion

#region Design
// Tone is a parameter on the tile, so a page does not restyle the card with ::deep.
// The modifier is a border color in KpiCard.razor.css, not a pill and not a shadow.
// Delta, trend, and chart slots stay off this enum.
#endregion

namespace TimeWarp.Architecture.Components;

public enum KpiTone
{
  None,
  Positive,
  Danger,
}
