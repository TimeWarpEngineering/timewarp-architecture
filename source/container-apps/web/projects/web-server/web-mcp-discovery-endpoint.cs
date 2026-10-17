#region Purpose
// Serves the generated WebMCP discovery document to HTTP clients that do not run JavaScript.
#endregion

#region Design
// Anonymous and stable: the document describes the signed-out destination list, not the caller's
// session. AllowAnonymous so a fallback policy cannot hide it. The MCP server card is a different
// route and stays a stub. Route rows use the SPA IActionCatalog (registered by Web.Spa.Program
// on this host) so human-only actions are omitted. This file is outside the agent sources so it
// may name the home path the document is built for.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.State;

/// <summary>GET the generated WebMCP discovery document.</summary>
public static class WebMcpDiscoveryEndpoint
{
  public static void Map(WebApplication webApplication)
  {
    webApplication.MapGet(WebMcpDiscoveryCatalog.DocumentPath, HandleAsync).AllowAnonymous();
  }

  private static async Task<IResult> HandleAsync
  (
    IAuthorizationService authorizationService,
    IActionCatalog actionCatalog
  )
  {
    ClaimsPrincipal anonymous = new(new ClaimsIdentity());
    string json = await WebMcpDiscoveryCatalog.DocumentJsonAsync
    (
      anonymous,
      authorizationService,
      actionCatalog.Entries,
      "/"
    );
    return Results.Content(json, MediaTypeNames.Application.Json);
  }
}
