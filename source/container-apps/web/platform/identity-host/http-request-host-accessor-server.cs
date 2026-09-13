#region Purpose
// IRequestHostAccessor implementation: reads the circuit host (internal header) when the request
// Host is loopback, otherwise the current request's host (port stripped) so identity handlers can
// select a WebAuthn RP ID per request.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), mirroring HttpCurrentPrincipalAccessor/
// CookieBrowserSessionService — see IRequestHostAccessor's Design region for why the port lives in
// web-application while this ASP.NET-Core-bound implementation lives in platform/identity-host
// (compiled into web-server via the -server suffix glob).
// HttpRequestHost.Host is the host WITHOUT the port (HostString exposes Host and Port separately),
// which is exactly the bare domain an RP ID must be. Behind the task-112 ingress this reads the
// PUBLIC host only because that ingress preserves the original Host header: the AppHost's YARP carves
// /api/identity/** -> Web.Server with WithTransformUseOriginalHostHeader, which is the verified
// task-112 public chain. (The standalone yarp project does NOT route /api/identity/** to Web.Server
// at all — a pre-existing routing gap unrelated to 104-031 — so passkey host-preservation there is
// moot; see that project's appsettings.Development.json.) No UseForwardedHeaders and no spoofable
// X-Forwarded-Host is consumed; a forged Host can at most select among the already-approved
// AllowedRpIds, never expand them. InteractiveServer/Auto named-HttpClient loopback is not a
// forwarded-header problem: IdentitySessionCookieForwardingHandler copies the circuit request's
// host (port stripped) onto X-TimeWarp-Circuit-Host. This accessor honors that internal header
// only when Request.Host.Host is loopback (localhost case-insensitive, or an IPAddress.IsLoopback
// address such as 127.0.0.1 / ::1) so HTTPS loopback TLS still validates localhost against the
// ASP.NET dev cert while RP-ID selection sees the circuit/page host. On the public path Host is
// not loopback, so a client-supplied copy of the header is ignored and Request.Host.Host wins.
// HTTP Host is left unset on the loopback hop. Loopback Host is localhost (or a loopback IP), so
// the header is the circuit/page host.
// Null-safe: no HttpContext (e.g. resolved outside a request) returns null rather than throwing,
// which the selection treats as a fail-closed "host not allowed" — same posture as
// HttpCurrentPrincipalAccessor's null return for no authenticated caller.
#endregion

namespace TimeWarp.Architecture.Services;

using System.Net;
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
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is null)
    {
      return null;
    }

    string? host = httpContext.Request.Host.Host;
    string circuitHost = httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader].ToString();
    if (!string.IsNullOrEmpty(circuitHost) && IsLoopbackHost(host))
    {
      return circuitHost;
    }

    return string.IsNullOrEmpty(host) ? null : host;
  }

  private static bool IsLoopbackHost(string? host)
  {
    if (string.IsNullOrEmpty(host))
    {
      return false;
    }

    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    string candidate = host;
    if (candidate.StartsWith('[') && candidate.EndsWith(']'))
    {
      candidate = candidate[1..^1];
    }

    return IPAddress.TryParse(candidate, out IPAddress? address) && IPAddress.IsLoopback(address);
  }
}
