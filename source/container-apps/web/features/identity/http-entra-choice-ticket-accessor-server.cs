#region Purpose
// HttpContext implementation of IEntraChoiceTicketAccessor.
#endregion

#region Design
// Scoped per request (IHttpContextAccessor). Missing HttpContext reads as no ticket.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Features.Identity.Application;

public sealed class HttpEntraChoiceTicketAccessor : IEntraChoiceTicketAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HttpEntraChoiceTicketAccessor(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public bool TryReadParkId(out string parkId)
  {
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is null)
    {
      parkId = "";
      return false;
    }

    parkId = httpContext.Request.Cookies[EntraChoiceCookie.CookieName] ?? "";
    return parkId.Length > 0;
  }

  public void Clear()
  {
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is null)
    {
      return;
    }

    httpContext.Response.Cookies.Delete(
      EntraChoiceCookie.CookieName,
      new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
      });
  }
}
