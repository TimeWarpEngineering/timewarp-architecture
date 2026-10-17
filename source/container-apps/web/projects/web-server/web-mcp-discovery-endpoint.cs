#region Purpose
// Serves the generated WebMCP discovery document to HTTP clients that do not run JavaScript.
#endregion

#region Design
// Anonymous and stable: the document describes the signed-out destination list, not the caller's
// session. AllowAnonymous so a fallback policy cannot hide it. The MCP server card is a different
// route and stays a stub. This file is outside the agent sources so it may name the home path
// the document is built for.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TimeWarp.Architecture.Features.Applications;

/// <summary>GET the generated WebMCP discovery document.</summary>
public static class WebMcpDiscoveryEndpoint
{
  public static void Map(WebApplication webApplication)
  {
    webApplication.MapGet(WebMcpDiscoveryCatalog.DocumentPath, HandleAsync).AllowAnonymous();
  }

  private static async Task<IResult> HandleAsync(IAuthorizationService authorizationService)
  {
    ClaimsPrincipal anonymous = new(new ClaimsIdentity());
    string json = await WebMcpDiscoveryCatalog.DocumentJsonAsync(anonymous, authorizationService, "/");
    return Results.Content(json, MediaTypeNames.Application.Json);
  }
}
