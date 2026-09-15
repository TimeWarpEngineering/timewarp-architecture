#region Purpose
// Sanitizes post-Entra redirect targets: local paths only, never /Login or the challenge route.
#endregion

#region Design
// Mirrors LoginPage.GetSafeReturnUrl (open-redirect + login-loop guards) on the server so the OIDC
// ticket completion redirect cannot be pointed at an absolute URL. Also refuses the challenge path
// itself so a crafted returnUrl cannot bounce the browser through Challenge again.
// AuthenticationProperties.RedirectUri is the local return path from the challenge query (e.g.
// /Profile). PublicOrigin overrides only the OIDC redirect_uri, so Sanitize still accepts that
// local path after callback when the public origin differs from the internal http origin.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Diagnostics.CodeAnalysis;

public static class LocalReturnUrl
{
  [SuppressMessage(
    "Design",
    "CA1054:URI-like parameters should not be strings",
    Justification = "OIDC AuthenticationProperties.RedirectUri and query-string returnUrl are local paths, not absolute URIs.")]
  public static string Sanitize(string? returnUrl)
  {
    if (string.IsNullOrEmpty(returnUrl)
      || !returnUrl.StartsWith('/')
      || returnUrl.StartsWith("//", StringComparison.Ordinal)
      || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
    {
      return "/";
    }

    string path = returnUrl.Split('?', '#')[0].TrimEnd('/');
    if (path.Equals(IdentitySessionCookieChallenge.LoginPath, StringComparison.OrdinalIgnoreCase)
      || path.Equals(ChallengeEntra.Path, StringComparison.OrdinalIgnoreCase))
    {
      return "/";
    }

    return returnUrl;
  }
}
