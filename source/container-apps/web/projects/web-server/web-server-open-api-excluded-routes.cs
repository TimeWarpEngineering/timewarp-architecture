#region Purpose
// Web-server routes that are served but are not OpenAPI operations, for x-timewarp-excluded-routes.
#endregion

#region Design
// Task 458-007: info.description says a missing path is an exclusion, so every route this host
// maps outside FastEndpoints is listed here or in OpenApiDiscoveryCoverage's shared list (the
// well-known documents, WebMCP, liveness). Each path is the constant the Map call uses, so a
// renamed route moves the entry with it. Not listed: Razor pages, MapStaticAssets files, Blazor
// framework routes, the OpenAPI document and Scalar, and Development-only routes (/health and the
// TimeWarp.State DevTools config); the document description names those families. The
// discovery-coverage test (WebServer_Given_) enumerates the real host's endpoints and fails on a
// route that is in none of these groups.
#endregion

namespace TimeWarp.Architecture.Web.Server;

/// <summary>
/// Non-operation web-server routes for the OpenAPI document.
/// </summary>
internal static class WebServerOpenApiExcludedRoutes
{
  internal const string SignalRNegotiateSuffix = "/negotiate";

  internal static readonly OpenApiExcludedRoute[] All =
  [
    new("GET", BrandStylesheet.Path, "Brand stylesheet (text/css) rendered from BrandOptions. Anonymous."),
    new("GET", BrandDocument.Path, "BrandOptions as application/json for the WebAssembly host. Anonymous."),
    new("GET", WebManifest.Path, "Web app manifest (" + WebManifest.ContentType + "). Anonymous."),
    new
    (
      "POST",
      ChatHubConstants.Route + SignalRNegotiateSuffix,
      "SignalR negotiate for the SPA chat hub. A SignalR client calls it; it is not REST."
    ),
    new("GET", ChatHubConstants.Route, "SignalR connection for the SPA chat hub (WebSocket, server-sent events, or long polling)."),
    new
    (
      "GET",
      TranscribeAgentAudio.StreamPath,
      "WebSocket for Ask dictation audio (CONNECT on HTTP/2). Requires the identity session. "
      + "A request that is not a WebSocket upgrade is 400."
    ),
  ];
}
