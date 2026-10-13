#region Purpose
// Identifies the WebAuthn Relying Party (this application) for ceremony options and verification.
#endregion

#region Design
// Id is the RP ID (typically the registrable domain, e.g. "localhost" or "timewarp.example") that
// authenticator-data.cs hashes and compares against; Name is the human-readable name shown by the
// platform's passkey UI.
// Origins is the allow-list of exact browser origins (scheme+host+port) the RP accepts. An empty
// list is a deliberate dev fallback, not an error: WebAuthnRegistration/WebAuthnAuthentication treat
// it as "accept any https origin whose host equals Id, and http only when that host is loopback"
// (localhost, 127.0.0.1, [::1]) — this covers the in-proc test-host origin
// (https://localhost:<InProcTestPorts base>; the port varies, never the host), the dev origin
// (https://localhost:63611) and a plain-http kubectl port-forward (http://localhost:<ingress-port>)
// without hand-listing every port, while still requiring the correct host. The http allowance
// mirrors the browser secure-context rule (browsers run WebAuthn on http://localhost); a
// non-loopback host still needs https. Production configuration should supply an explicit Origins
// list; a non-empty list is an exact, ordinal match with no fallback.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Relying-party identity used when building WebAuthn options and verifying ceremony responses.
/// </summary>
/// <param name="Id">RP ID (typically the registrable domain) hashed into authenticator data.</param>
/// <param name="Name">Human-readable name shown by the platform passkey UI.</param>
/// <param name="Origins">Exact origin allow-list; empty accepts an https origin whose host equals <paramref name="Id"/>, or http when that host is loopback.</param>
public sealed record WebAuthnRelyingParty(
  /// <summary>RP ID (typically the registrable domain) hashed into authenticator data.</summary>
  string Id,
  /// <summary>Human-readable name shown by the platform passkey UI.</summary>
  string Name,
  /// <summary>Exact origin allow-list; empty accepts an https origin whose host equals <see cref="Id"/>, or http when that host is loopback.</summary>
  IReadOnlyList<string> Origins);
