#region Purpose
// Copies inbound Cookie and mock-principal headers onto server HttpClient loopback.
#endregion

#region Design
// InteractiveAuto/InteractiveServer named HttpClient loopbacks to web-server without the
// browser Cookie (task 183). GET GetProfile is AllowAnonymous so the Profile form still
// loads; PUT UpdateProfile is [EndpointAuthorize] profile.write and challenges 401.
// IHttpContextAccessor is AsyncLocal-safe on a pooled DelegatingHandler. Copies Cookie as
// sent and X-TimeWarp-Mock-Principal-Id so mock-identity-session still authenticates when
// that header is in play. Does not invent cookies — missing inbound Cookie stays anonymous.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TimeWarp.Architecture.Services;

/// <summary>
/// Forwards the browser identity-session cookie (and mock principal header) on server loopback.
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
}
