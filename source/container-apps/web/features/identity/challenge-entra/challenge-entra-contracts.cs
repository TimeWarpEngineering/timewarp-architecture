#region Purpose
// Route helper for the BFF Entra challenge URL used by the SPA and the hand-written FastEndpoint.
#endregion

#region Design
// Not [ApiRoute]/[ApiEndpoint]: the response is an OIDC Challenge redirect (or 401/403/404), not
// JSON from a generated FastEndpoint. TWA0006 would flag an [ApiRoute] with no generated host
// endpoint. The path string is the SSOT for LoginPage and ChallengeEntraEndpoint.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static class ChallengeEntra
{
  public const string Path = "/api/identity/entra/challenge";

  public static string GetRoute(string mode, string? returnPath)
  {
    ArgumentException.ThrowIfNullOrEmpty(mode);
    string route = $"{Path}?mode={Uri.EscapeDataString(mode)}";
    if (!string.IsNullOrEmpty(returnPath))
    {
      route += $"&returnUrl={Uri.EscapeDataString(returnPath)}";
    }

    return route;
  }
}
