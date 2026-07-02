#region Purpose
// Names the Auth feature group as a constant for tagging its endpoints in API metadata; no code consumes it.
#endregion

namespace TimeWarp.Architecture.Features.Auth;

public static class FeatureAnnotations
{
  public const string FeatureGroup = "Auth";
}
