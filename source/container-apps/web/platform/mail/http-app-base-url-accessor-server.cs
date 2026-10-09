#region Purpose
// Reads scheme, host, and path base from the current HTTP request.
#endregion

#region Design
// Returns null when there is no request or no host, so mail falls back to the relative permalink.
// Path base is included when the app is mounted under one.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class HttpAppBaseUrlAccessor : IAppBaseUrlAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HttpAppBaseUrlAccessor(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public Uri? GetBaseUrl()
  {
    if (HttpContextAccessor.HttpContext?.Request is not { Host.HasValue: true } request)
    {
      return null;
    }

    string pathBase = request.PathBase.Value ?? string.Empty;
    return new Uri($"{request.Scheme}://{request.Host.Value}{pathBase}");
  }
}
