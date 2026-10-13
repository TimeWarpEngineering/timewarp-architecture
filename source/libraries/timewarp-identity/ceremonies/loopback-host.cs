#region Purpose
// The one loopback-host predicate for origin checks: which hosts may use plain http.
#endregion

#region Design
// Browsers treat http://localhost, http://127.0.0.1 and http://[::1] as secure contexts, so WebAuthn
// and OIDC redirects work there without TLS. Callers that allow http only on loopback use this, so the
// rule has one copy: WebAuthnOrigin's empty-Origins fallback and the web host's Entra PublicOrigin
// validator. It lives in TimeWarp.Identity because that is the lowest layer both share.
// The input is Uri.Host, which keeps the brackets on an IPv6 literal ("[::1]"); the bare "::1" form
// is accepted too so a caller that strips brackets gets the same answer. Only the canonical
// loopback names count — not other 127.0.0.0/8 addresses and not *.localhost subdomains — because
// that is what the template's dev and port-forward origins use, and widening is cheap later.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Decides whether a URI host is a loopback host, where browsers allow plain http as a secure context.
/// </summary>
public static class LoopbackHost
{
  /// <summary>
  /// True for <c>localhost</c> (any case), <c>127.0.0.1</c>, and <c>[::1]</c> (as <see cref="Uri.Host"/> returns it).
  /// </summary>
  /// <param name="host">The host component of an origin, for example <see cref="Uri.Host"/>.</param>
  public static bool IsLoopback(string? host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
    || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
    || string.Equals(host, "[::1]", StringComparison.Ordinal)
    || string.Equals(host, "::1", StringComparison.Ordinal);
}
