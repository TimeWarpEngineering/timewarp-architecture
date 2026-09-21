#region Purpose
// Identifies the WebAuthn Relying Party (this application) for ceremony options and verification.
#endregion

#region Design
// Id is the RP ID (typically the registrable domain, e.g. "localhost" or "timewarp.example") that
// authenticator-data.cs hashes and compares against; Name is the human-readable name shown by the
// platform's passkey UI.
// Origins is the allow-list of exact browser origins (scheme+host+port) the RP accepts. An empty
// list is a deliberate dev fallback, not an error: WebAuthnRegistration/WebAuthnAuthentication treat
// it as "accept any https origin whose host equals Id" — this covers the fixed test-host origin
// (https://localhost:7000) and the dev origin (https://localhost:63611) without hand-listing every
// port a developer might run on, while still requiring https and the correct host. Production
// configuration should supply an explicit Origins list.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Relying-party identity used when building WebAuthn options and verifying ceremony responses.
/// </summary>
/// <param name="Id">RP ID (typically the registrable domain) hashed into authenticator data.</param>
/// <param name="Name">Human-readable name shown by the platform passkey UI.</param>
/// <param name="Origins">Exact origin allow-list; empty accepts any https origin whose host equals <paramref name="Id"/>.</param>
public sealed record WebAuthnRelyingParty(
  /// <summary>RP ID (typically the registrable domain) hashed into authenticator data.</summary>
  string Id,
  /// <summary>Human-readable name shown by the platform passkey UI.</summary>
  string Name,
  /// <summary>Exact origin allow-list; empty accepts any https origin whose host equals <see cref="Id"/>.</summary>
  IReadOnlyList<string> Origins);
