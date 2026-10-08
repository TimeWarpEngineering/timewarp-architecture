#region Purpose
// IRequestHostAccessor implementation: reads the circuit host (internal header) when the request
// Host is loopback, otherwise the public host (X-Forwarded-Host, else Request.Host; port stripped)
// so identity handlers can select a WebAuthn RP ID per request.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), mirroring HttpCurrentPrincipalAccessor/
// CookieBrowserSessionService — see IRequestHostAccessor's Design region for why the port lives in
// web-application while this ASP.NET-Core-bound implementation lives in platform/identity-host
// (compiled into web-server via the -server suffix glob).
// Public host = X-Forwarded-Host, else Request.Host (task 070-008). On every target (run, Compose,
// Kubernetes, ACA) the YARP ingress sends the DESTINATION host as Host — ACA's internal ingress
// routes app-to-app traffic by Host, so a public Host matches no app — and SETS X-Forwarded-Host
// to the browser's public host. Only the first value counts (first header value, then first
// comma-separated entry), with the port stripped via HostString.Host: an RP ID is a bare domain.
// Request.Host is the fallback when the header is absent (web-server reached directly, e.g.
// https://localhost:7000 in run mode). Why reading this header is safe — it reverses 104-031's
// "no X-Forwarded-Host is consumed" stance, so the argument is recorded here:
//   1. YARP overwrites, never appends: every Web.Server route carries the X-Forwarded transform with
//      action Set, which drops any client-supplied X-Forwarded-Host (aspire-tests ingress smoke
//      sends a forged one and asserts web-server sees the real public host).
//   2. Selection only, never expansion: the value only SELECTS among the operator-approved
//      WebAuthnOptions.AllowedRpIds, exactly as Host did. A forged value cannot mint a credential
//      for an RP ID the operator did not approve; absent or unapproved means "host not allowed".
//   3. Narrow consumption: this accessor (and IdentitySessionCookieForwardingHandler, which copies
//      GetPublicHost onto the loopback hop) is the only reader. No UseForwardedHeaders — that would
//      also rewrite scheme and remote IP. Entra keeps its explicit PublicOrigin.
//   4. Posture equals the old one: where web-server is directly reachable (run mode), a client can
//      send its own X-Forwarded-Host just as it could always send its own Host. Point 2 is what makes
//      both safe; the forwarded host is never trusted input, only a selector.
// InteractiveServer/Auto named-HttpClient loopback: IdentitySessionCookieForwardingHandler copies
// the circuit request's public host onto X-TimeWarp-Circuit-Host. This accessor honors that internal
// header only when Request.Host.Host is loopback (localhost case-insensitive, or an
// IPAddress.IsLoopback address such as 127.0.0.1 / ::1) so HTTPS loopback TLS still validates
// localhost against the ASP.NET dev cert while RP-ID selection sees the circuit/page host. On the
// public path Host is not loopback, so a client-supplied copy of the header is ignored.
// HTTP Host is left unset on the loopback hop.
// Null-safe: no HttpContext (e.g. resolved outside a request) returns null rather than throwing,
// which the selection treats as a fail-closed "host not allowed" — same posture as
// HttpCurrentPrincipalAccessor's null return for no authenticated caller.
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.HttpOverrides;
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

    string circuitHost = httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader].ToString();
    if (!string.IsNullOrEmpty(circuitHost) && IsLoopbackHost(httpContext.Request.Host.Host))
    {
      return circuitHost;
    }

    return GetPublicHost(httpContext.Request);
  }

  /// <summary>
  /// The browser's public host: the first X-Forwarded-Host value (port stripped), else Request.Host.
  /// Selects among approved RP IDs only — never trusted input.
  /// </summary>
  public static string? GetPublicHost(HttpRequest request)
  {
    string? forwardedHost = request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName].FirstOrDefault()?.Split(',')[0].Trim();
    string? host = string.IsNullOrEmpty(forwardedHost) ? request.Host.Host : new HostString(forwardedHost).Host;

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
    if (candidate.StartsWith('[', StringComparison.Ordinal) && candidate.EndsWith(']', StringComparison.Ordinal))
    {
      candidate = candidate[1..^1];
    }

    return IPAddress.TryParse(candidate, out IPAddress? address) && IPAddress.IsLoopback(address);
  }
}
