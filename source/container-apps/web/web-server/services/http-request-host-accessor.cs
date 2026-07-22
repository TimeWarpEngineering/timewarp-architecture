#region Purpose
// IRequestHostAccessor implementation: reads the current request's host (port stripped) off
// HttpContext.Request.Host so the identity handlers can select a WebAuthn RP ID per request.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), mirroring HttpCurrentPrincipalAccessor/
// CookieBrowserSessionService — see IRequestHostAccessor's Design region for why the port lives in
// web-application while this ASP.NET-Core-bound implementation lives in web-server.
// HttpRequestHost.Host is the host WITHOUT the port (HostString exposes Host and Port separately),
// which is exactly the bare domain an RP ID must be. Behind the task-112 ingress this reads the
// PUBLIC host only because the ingress preserves the original Host header (AppHost
// WithTransformUseOriginalHostHeader / standalone yarp RequestHeaderOriginalHost) — no
// UseForwardedHeaders and no spoofable X-Forwarded-Host is consumed; a forged Host can at most select
// among the already-approved AllowedRpIds, never expand them.
// Null-safe: no HttpContext (e.g. resolved outside a request) returns null rather than throwing,
// which the selection treats as a fail-closed "host not allowed" — same posture as
// HttpCurrentPrincipalAccessor's null return for no authenticated caller.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Abstractions;

public sealed class HttpRequestHostAccessor : IRequestHostAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HttpRequestHostAccessor(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public string? GetRequestHost()
  {
    string? host = HttpContextAccessor.HttpContext?.Request.Host.Host;
    return string.IsNullOrEmpty(host) ? null : host;
  }
}
