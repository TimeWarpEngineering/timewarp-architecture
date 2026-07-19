#region Purpose
// Shared origin-check used by both WebAuthnRegistration.Verify and WebAuthnAuthentication.Verify.
#endregion

#region Design
// See WebAuthnRelyingParty's Design region for the empty-Origins dev-fallback rationale — this is
// the code that implements it: exact allow-list match when Origins is non-empty; otherwise accept
// any https origin whose host equals the RP id.
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

    return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
      && string.Equals(uri.Host, rp.Id, StringComparison.OrdinalIgnoreCase);
  }
}
