#region Purpose
// Names the Hello feature group as a constant for tagging its endpoints in API metadata; no code consumes it.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

public static class FeatureAnnotations
{
  public const string FeatureGroup = "Hello";
}
