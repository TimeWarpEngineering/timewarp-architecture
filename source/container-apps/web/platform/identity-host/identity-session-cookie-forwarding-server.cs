#region Purpose
// Copies inbound Cookie, mock-principal, User-Agent, and public origin onto the server HttpClient self-call.
#endregion

#region Design
// InteractiveAuto/InteractiveServer named HttpClient self-calls web-server without the
// browser Cookie (task 183). GET GetProfile is AllowAnonymous so the Profile form still
// loads; PUT UpdateProfile is [EndpointAuthorize] profile.write and challenges 401.
// IHttpContextAccessor is AsyncLocal-safe on a pooled DelegatingHandler. Copies Cookie as
// sent and X-TimeWarp-Mock-Principal-Id so mock-identity-session still authenticates when
// that header is in play. Does not invent cookies — missing inbound Cookie stays anonymous.
// Task 213 / 312: copies the circuit request's full public origin onto the self-call — the
// authority (port kept) as X-Forwarded-Host and the scheme as X-Forwarded-Proto — taken from
// HttpAppBaseUrlAccessor.GetRequestOrigin (first forwarded proto, else Request.Scheme; first
// forwarded host, else Request.Host). The self-call reaches the discovered address (https://localhost
// in run mode, http://web-server published), so without these headers the hop would see that
// address instead of the browser's. Two readers consume them on the hop: HttpRequestHostAccessor
// strips the port for WebAuthn RP-ID selection, and HttpAppBaseUrlAccessor / LinkPreview build
// absolute URLs (mail permalinks) and need the port and scheme intact. On published targets Aspire
// enables the forwarded-headers middleware (X-Forwarded-For/Proto only), so the forwarded proto also
// sets Request.Scheme on the self-call to the public scheme — intended: the hop acts for the browser.
// Each header is set only when the outgoing request does not already carry it. Does not set HTTP
// Host — HttpClient uses Host for TLS SNI / certificate name validation on the https hop.
// Task 248-001: copies User-Agent as sent so HttpRequestUserAgentAccessor on the self-call hop
// sees the registering browser (credential rows record its browser/OS family), not the
// server-side HttpClient. Same copy-as-sent / never-invent posture as Cookie.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using TimeWarp.Architecture.Mail;
using TimeWarp.Architecture.Services;

/// <summary>
/// Forwards the browser identity-session cookie, mock principal header, User-Agent, and public origin on the server self-call.
/// </summary>
public sealed class IdentitySessionCookieForwardingHandler : DelegatingHandler
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public IdentitySessionCookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor
      ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is not null)
    {
      CopyHeader(httpContext, request, "Cookie");
      CopyHeader(httpContext, request, MockAuthenticationDefaults.MockPrincipalIdHeader);
      // Task 248-001: the registering browser's User-Agent must survive the self-call hop so the
      // credential row records the real browser/OS family, not the server's HttpClient.
      CopyHeader(httpContext, request, "User-Agent");
      ForwardPublicOrigin(httpContext, request);
    }

    return base.SendAsync(request, cancellationToken);
  }

  private static void CopyHeader(HttpContext httpContext, HttpRequestMessage request, string headerName)
  {
    if (request.Headers.Contains(headerName))
    {
      return;
    }

    if (!httpContext.Request.Headers.TryGetValue(headerName, out StringValues values)
      || StringValues.IsNullOrEmpty(values))
    {
      return;
    }

    request.Headers.TryAddWithoutValidation(headerName, values.ToArray());
  }

  private static void ForwardPublicOrigin(HttpContext httpContext, HttpRequestMessage request)
  {
    Uri? origin = HttpAppBaseUrlAccessor.GetRequestOrigin(httpContext.Request);
    if (origin is null)
    {
      return;
    }

    if (!request.Headers.Contains(ForwardedHeadersDefaults.XForwardedHostHeaderName))
    {
      request.Headers.TryAddWithoutValidation(ForwardedHeadersDefaults.XForwardedHostHeaderName, origin.Authority);
    }

    if (!request.Headers.Contains(ForwardedHeadersDefaults.XForwardedProtoHeaderName))
    {
      request.Headers.TryAddWithoutValidation(ForwardedHeadersDefaults.XForwardedProtoHeaderName, origin.Scheme);
    }
  }
}
