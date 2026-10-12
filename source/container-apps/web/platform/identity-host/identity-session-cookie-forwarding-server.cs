#region Purpose
// Copies inbound Cookie, mock-principal, User-Agent, and public host onto the server HttpClient self-call.
#endregion

#region Design
// InteractiveAuto/InteractiveServer named HttpClient loopbacks to web-server without the
// browser Cookie (task 183). GET GetProfile is AllowAnonymous so the Profile form still
// loads; PUT UpdateProfile is [EndpointAuthorize] profile.write and challenges 401.
// IHttpContextAccessor is AsyncLocal-safe on a pooled DelegatingHandler. Copies Cookie as
// sent and X-TimeWarp-Mock-Principal-Id so mock-identity-session still authenticates when
// that header is in play. Does not invent cookies — missing inbound Cookie stays anonymous.
// Task 213 / 312: copies the circuit request's public host (HttpRequestHostAccessor.GetPublicHost:
// the ingress-set X-Forwarded-Host, else Request.Host; port stripped) onto X-Forwarded-Host so
// WebAuthn RP-ID selection on the self-call sees the browser's host, not the discovered one
// (https://localhost in run mode, http://web-server published). The accessor reads that header on
// every hop with the same selection-only argument (its Design region). Does not set HTTP Host —
// HttpClient uses Host for TLS SNI / certificate name validation on the https hop. Does not
// overwrite an X-Forwarded-Host the outgoing request already set.
// Task 248-001: copies User-Agent as sent so HttpRequestUserAgentAccessor on the loopback hop
// sees the registering browser (credential rows record its browser/OS family), not the
// server-side HttpClient. Same copy-as-sent / never-invent posture as Cookie.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using TimeWarp.Architecture.Services;

/// <summary>
/// Forwards the browser identity-session cookie, mock principal header, User-Agent, and public host on the server self-call.
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
      // Task 248-001: the registering browser's User-Agent must survive the loopback hop so the
      // credential row records the real browser/OS family, not the server's HttpClient.
      CopyHeader(httpContext, request, "User-Agent");
      ForwardPublicHost(httpContext, request);
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

  private static void ForwardPublicHost(HttpContext httpContext, HttpRequestMessage request)
  {
    if (request.Headers.Contains(ForwardedHeadersDefaults.XForwardedHostHeaderName))
    {
      return;
    }

    string? host = HttpRequestHostAccessor.GetPublicHost(httpContext.Request);
    if (string.IsNullOrEmpty(host))
    {
      return;
    }

    request.Headers.TryAddWithoutValidation(ForwardedHeadersDefaults.XForwardedHostHeaderName, host);
  }
}
