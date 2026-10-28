#region Purpose
// One line on a LineChart: a label, a ChartSeriesTone, and the point values.
#endregion

#region Design
// The page formats axis labels and chooses the tone. Points are positions only.
// This record does not name a currency, a date, or a color.
#endregion

namespace TimeWarp.Architecture.Components;

public sealed record LineChartSeries(string Label, ChartSeriesTone Tone, IReadOnlyList<double> Points);
