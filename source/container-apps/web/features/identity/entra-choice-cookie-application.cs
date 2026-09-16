#region Purpose
// Cookie name, choose-page path, and TTL for parked Entra bootstrap claims.
#endregion

#region Design
// Cookie holds only the opaque park id. TTL matches the in-memory park (10 min). Path=/ so
// /api/identity/entra/choice* sees the cookie set on the OIDC callback. HTTP get/set lives on
// IEntraChoiceTicketAccessor so application handlers stay free of HttpContext.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public static class EntraChoiceCookie
{
  public const string CookieName = ".Tw.EntraChoice";
  public const string ChoosePath = "/Login/Microsoft365/Choose";
  public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
}
