#region Purpose
// Names the Analytics feature group as a constant for tagging its endpoints in API metadata; no code consumes it.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

public static class FeatureAnnotations
{
  public const string FeatureGroup = "Analytics";
}
