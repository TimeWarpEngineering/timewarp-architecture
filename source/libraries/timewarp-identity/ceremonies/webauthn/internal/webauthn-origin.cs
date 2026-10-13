#region Purpose
// Shared origin-check used by both WebAuthnRegistration.Verify and WebAuthnAuthentication.Verify.
#endregion

#region Design
// See WebAuthnRelyingParty's Design region for the empty-Origins dev-fallback rationale — this is
// the code that implements it: exact, ordinal allow-list match when Origins is non-empty; otherwise
// accept an origin whose host equals the RP id when its scheme is https, or http when that host is
// loopback (LoopbackHost). The http case mirrors the browser secure-context rule: a browser runs
// WebAuthn on http://localhost, so a plain-http port-forward (dev open --target kubernetes) must
// verify too. http on any other host is never accepted.
#endregion

namespace TimeWarp.Identity;

internal static class WebAuthnOrigin
{
  public static bool IsAllowed(WebAuthnRelyingParty rp, string? origin)
  {
    if (string.IsNullOrEmpty(origin)) return false;

    if (rp.Origins.Count > 0)
    {
      return rp.Origins.Contains(origin, StringComparer.Ordinal);
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)) return false;

    if (!string.Equals(uri.Host, rp.Id, StringComparison.OrdinalIgnoreCase)) return false;

    return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
      || (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) && LoopbackHost.IsLoopback(uri.Host));
  }
}
