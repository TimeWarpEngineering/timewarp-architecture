#region Purpose
// IRequestHostAccessor implementation: reads the public host (X-Forwarded-Host, else Request.Host;
// port stripped) so identity handlers can select a WebAuthn RP ID per request.
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
//      action Set. The value it writes is the ingress request's host after trusted-proxy forwarding
//      (task 305). A peer that is not a configured trusted proxy cannot inject X-Forwarded-Host.
//      Loopback is trusted (Caddy on this machine, and in-proc clients), so that peer's
//      X-Forwarded-Host is what Set forwards. The standalone ingress smoke covers the untrusted peer.
//   2. Selection only, never expansion: the value only SELECTS among the operator-approved
//      WebAuthnOptions.AllowedRpIds, exactly as Host did. A forged value cannot mint a credential
//      for an RP ID the operator did not approve; absent or unapproved means "host not allowed".
//   3. Narrow consumption: X-Forwarded-Host is read by this accessor (RP-ID selection, port
//      stripped), by HttpAppBaseUrlAccessor.GetRequestOrigin (absolute mail/link-preview URLs, port
//      kept), and by IdentitySessionCookieForwardingHandler, which copies that origin onto the
//      self-call hop. None of them rewrites Request.Host. Web.Server never calls UseForwardedHeaders
//      itself. Aspire does set
//      ASPNETCORE_FORWARDEDHEADERS_ENABLED=true on every project resource with endpoints in
//      publish mode only (Compose/Kubernetes/ACA output; Aspire.Hosting 13.6). That turns on ASP.NET Core's ForwardedHeaders middleware
//      for X-Forwarded-For and X-Forwarded-Proto only — not X-Forwarded-Host — with the known
//      proxies/networks cleared, so Request.Scheme and RemoteIpAddress follow the ingress's
//      headers while Request.Host stays the destination. Entra keeps its explicit PublicOrigin.
//   4. Posture equals the old one: where web-server is directly reachable (run mode), a client can
//      send its own X-Forwarded-Host just as it could always send its own Host. Point 2 is what makes
//      both safe; the forwarded host is never trusted input, only a selector.
// InteractiveServer/Auto self-call (task 312): the web-server named HttpClient reaches this app
// through service discovery — https://localhost in run mode, http://web-server on published
// targets (not loopback). IdentitySessionCookieForwardingHandler sets X-Forwarded-Host (port kept)
// and X-Forwarded-Proto on that hop to the circuit request's public origin; this accessor strips the
// port again, so this one rule serves both hops. The retired
// X-TimeWarp-Circuit-Host header was honored only on a loopback Host and so failed on published
// targets; X-Forwarded-Host needs no extra trust (points 1, 2 and 4). HTTP Host is left unset on
// the hop so TLS keeps validating the discovered host.
// Null-safe: no HttpContext (e.g. resolved outside a request) returns null rather than throwing,
// which the selection treats as a fail-closed "host not allowed" — same posture as
// HttpCurrentPrincipalAccessor's null return for no authenticated caller.
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.HttpOverrides;
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
}
