#region Purpose
// Route helper for the BFF Entra challenge URL used by the SPA and the hand-written FastEndpoint.
#endregion

#region Design
// Not [ApiRoute]/[ApiEndpoint]: the response is an OIDC Challenge redirect (or 401/403/404), not
// JSON from a generated FastEndpoint. TWA0006 would flag an [ApiRoute] with no generated host
// endpoint. The path string is the SSOT for LoginPage and ChallengeEntraEndpoint. With no
// [Operation] to generate them, OperationName and OperationDescription are declared here and the
// endpoint passes them as OpenApiOperationName metadata (task 458-005).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static class ChallengeEntra
{
  /// <summary>The OpenAPI operation id. The endpoint is hand-written, so no [Operation] generates it.</summary>
  public const string OperationName = "entra.challenge";

  /// <summary>The OpenAPI summary of <see cref="OperationName"/>.</summary>
  public const string OperationDescription = "Start a Microsoft 365 sign-in or link by redirecting to Entra.";

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
