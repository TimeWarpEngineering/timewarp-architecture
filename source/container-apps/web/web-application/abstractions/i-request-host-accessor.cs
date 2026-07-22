#region Purpose
// Port for reading the current request's host name (the Host header, port stripped) so the identity
// handlers can select a WebAuthn RP ID per request without web-application depending on ASP.NET Core.
#endregion

#region Design
// Task 104-031: passkey ceremonies select their RP ID from the request host against an allowlist
// (see WebAuthnRelyingPartySelection.Select). The handlers that do this live in web-application,
// which deliberately has NO ASP.NET Core reference — the same layering constraint that gave rise to
// ICurrentPrincipalAccessor (impl in web-server via IHttpContextAccessor). This port is that same
// pattern: a scheme-agnostic host read that web-application declares and web-server implements
// (HttpRequestHostAccessor) off HttpContext.Request.Host.Host.
// Returns the HOST ONLY (no port): an RP ID is a bare domain, and WebAuthnRelyingPartySelection
// matches it against AllowedRpIds entries which are validated as bare DNS names. Returns null (never
// throws) when there is no active HTTP request or no Host — callers treat null as "host not allowed"
// and fail closed, exactly as they treat an unlisted host (same defense-in-depth posture as
// ICurrentPrincipalAccessor returning null for no authenticated caller).
// Synchronous (unlike ICurrentPrincipalAccessor's Task-returning shape): reading Request.Host is a
// pure property access with no I/O and no per-scheme authenticate call to mirror, so a plain string?
// return is the honest signature rather than async-for-uniformity.
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IRequestHostAccessor
{
  string? GetRequestHost();
}
