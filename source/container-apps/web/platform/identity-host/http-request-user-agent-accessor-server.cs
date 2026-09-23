#region Purpose
// IRequestUserAgentAccessor implementation: reads the current request's User-Agent header (null when
// there is no request or no header).
#endregion

#region Design
// Scoped over IHttpContextAccessor, mirroring HttpRequestHostAccessor. Returns the header verbatim;
// RegistrationContext caps and classifies it. No forwarded-header handling is needed: User-Agent is
// end-to-end (the task-112 ingress passes it through untouched, and the InteractiveServer loopback
// hop copies it via IdentitySessionCookieForwardingHandler).
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Abstractions;

public sealed class HttpRequestUserAgentAccessor : IRequestUserAgentAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HttpRequestUserAgentAccessor(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public string? GetUserAgent()
  {
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is null)
    {
      return null;
    }

    string userAgent = httpContext.Request.Headers.UserAgent.ToString();
    return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
  }
}
