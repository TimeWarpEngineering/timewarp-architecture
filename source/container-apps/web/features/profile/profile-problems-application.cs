#region Purpose
// Shared SharedProblemDetails factories for profile application handlers.
#endregion

#region Design
// Slice-local (internal static) so other product slices cannot take a TWA0009 dependency on
// Profiles.Application. Unauthenticated is defense-in-depth: UpdateProfile is [EndpointAuthorize].
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

internal static class ProfileProblems
{
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };
}
