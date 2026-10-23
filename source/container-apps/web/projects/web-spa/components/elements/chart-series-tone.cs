#region Purpose
// Dot color for one ChartLegendItem: ink, primary, or positive.
#endregion

#region Design
// Three tones cover a small multi-series legend without naming a domain.
// Ink, primary, and positive are the template's own colors. A page that needs
// a fourth series uses a different label, not a new tone, until a chart asks.
#endregion

namespace TimeWarp.Architecture.Components;

public enum ChartSeriesTone
{
  Ink,
  Primary,
  Positive,
}
