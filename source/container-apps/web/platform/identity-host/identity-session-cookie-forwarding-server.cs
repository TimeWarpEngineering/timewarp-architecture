#region Purpose
// Copies inbound Cookie, mock-principal, and circuit host onto server HttpClient loopback.
#endregion

#region Design
// InteractiveAuto/InteractiveServer named HttpClient loopbacks to web-server without the
// browser Cookie (task 183). GET GetProfile is AllowAnonymous so the Profile form still
// loads; PUT UpdateProfile is [EndpointAuthorize] profile.write and challenges 401.
// IHttpContextAccessor is AsyncLocal-safe on a pooled DelegatingHandler. Copies Cookie as
// sent and X-TimeWarp-Mock-Principal-Id so mock-identity-session still authenticates when
// that header is in play. Does not invent cookies — missing inbound Cookie stays anonymous.
// Task 213: copies the circuit/page host (port stripped via HostString.Host) onto
// X-TimeWarp-Circuit-Host so HttpRequestHostAccessor / WebAuthn RP-ID selection sees the
// YARP-preserved browser host rather than the loopback URI host (localhost). Does not set
// HTTP Host on the outgoing HTTPS request — HttpClient uses Host for TLS SNI / certificate
// name validation, and the loopback cert is the ASP.NET dev cert for localhost. Does not
// read X-Forwarded-Host — a spoofable client header; the circuit host is taken from
// HttpContext.Request.Host of the circuit request, same source as Cookie. Does not overwrite
// X-TimeWarp-Circuit-Host when the outgoing request already set it.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TimeWarp.Architecture.Services;

/// <summary>
/// Forwards the browser identity-session cookie, mock principal header, and circuit host on server loopback.
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
      CopyCircuitHost(httpContext, request);
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

  private static void CopyCircuitHost(HttpContext httpContext, HttpRequestMessage request)
  {
    if (request.Headers.Contains(MockAuthenticationDefaults.CircuitHostHeader))
    {
      return;
    }

    string? host = httpContext.Request.Host.Host;
    if (string.IsNullOrEmpty(host))
    {
      return;
    }

    request.Headers.TryAddWithoutValidation(MockAuthenticationDefaults.CircuitHostHeader, host);
  }
}
