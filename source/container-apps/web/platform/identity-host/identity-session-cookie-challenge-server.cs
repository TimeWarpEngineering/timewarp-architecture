#region Purpose
// Classifies identity-session cookie challenges: HTML/page deep links 302 to Login; /api stays 401.
#endregion

#region Design
// Task 154 SSOT for dual-mode cookie challenge (server-side HTML redirect — not SPA-shell serve).
// Root cause: OnRedirectToLogin was unconditional 401, so signed-out address-bar hits to [Authorize]
// pages never reached the SPA or task-153 RedirectToLogin. Cookie events already own the response;
// shell-serve would require dropping page [Authorize] metadata (blast radius). Strategy:
//   | Request class | Unauthenticated challenge | Authenticated forbid        |
//   | /api/…        | 401                       | 403                         |
//   | Non-API       | 302 → /Login?returnUrl=…  | 403 (never Login — no loop) |
// ShouldRedirectToLogin: hard-stop if path starts with /api (contract seam must not HTML-redirect);
// else true for Sec-Fetch-Dest: document, Accept containing text/html, or non-api fallback (bare
// curl without fetch metadata). BuildLoginRedirectTarget uses lowercase returnUrl (LoginPage
// SupplyParameterFromQuery) and path+query only (PathBase+Path+QueryString — never absolute
// origin; GetSafeReturnUrl open-redirect guard stays the client SSOT for post-sign-in). Forbid
// always stays 403 in Program cookie events — insufficient policy is not "sign in again."
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Pure helpers for identity-session cookie <c>OnRedirectToLogin</c> dual-mode classification.
/// </summary>
public static class IdentitySessionCookieChallenge
{
  /// <summary>Login page route matching <c>[Page("/Login")]</c>.</summary>
  public const string LoginPath = "/Login";

  /// <summary>Query name matching LoginPage <c>SupplyParameterFromQuery(Name = "returnUrl")</c>.</summary>
  public const string ReturnUrlQueryParameter = "returnUrl";

  /// <summary>
  /// Whether an unauthenticated challenge should 302 to Login instead of returning 401.
  /// </summary>
  public static bool ShouldRedirectToLogin(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    // Hard stop: contract seam never HTML-redirects.
    if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    // Browser document navigation (address bar, full page load).
    string? secFetchDest = request.Headers["Sec-Fetch-Dest"];
    if (string.Equals(secFetchDest, "document", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    // Explicit HTML negotiation (curl -H 'Accept: text/html', many browsers).
    string accept = request.Headers.Accept.ToString();
    if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    // Non-API fallback: bare curl / tools that send neither fetch metadata nor Accept.
    return true;
  }

  /// <summary>
  /// Builds <c>/Login?returnUrl={escaped path+query}</c> for the current request.
  /// </summary>
  public static string BuildLoginRedirectTarget(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    string returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
    return $"{LoginPath}?{ReturnUrlQueryParameter}={Uri.EscapeDataString(returnUrl)}";
  }
}
