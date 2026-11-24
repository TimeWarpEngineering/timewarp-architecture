#region Purpose
// Serves the configuration-driven brand documents: /brand.css, /brand.json, and /manifest.json.
#endregion

#region Design
// Anonymous: the mark, the icons, and the manifest load before anyone signs in. None is a
// static asset, so a configuration edit is the next GET. Each sends Cache-Control: no-cache
// plus a strong ETag; If-None-Match uses weak comparison, so the strong tag and the same tag
// with a leading W/ are both 304 (BrandStylesheet.Revalidate).
// /brand.css is linked from App.razor after tokens.css. /manifest.json is the PWA manifest
// App.razor links. /brand.json is the BrandOptions section the WebAssembly host loads at boot
// (task 513: the server is the one BrandOptions source).
#endregion

namespace TimeWarp.Architecture.Web.Server;

/// <summary>GET routes for documents built from <see cref="BrandOptions"/>.</summary>
internal static class BrandEndpoints
{
  /// <summary>Maps the three brand documents, anonymous.</summary>
  internal static void Map(WebApplication webApplication)
  {
    MapDocument(webApplication, BrandStylesheet.Path, "text/css; charset=utf-8", BrandStylesheet.Render);
    MapDocument(webApplication, BrandDocument.Path, "application/json; charset=utf-8", BrandDocument.Render);
    MapDocument(webApplication, WebManifest.Path, WebManifest.ContentType, WebManifest.Render);
  }

  private static void MapDocument
  (
    WebApplication webApplication,
    string path,
    string contentType,
    Func<BrandOptions, string> render
  )
  {
    webApplication.MapGet
    (
      path,
      (HttpContext httpContext, IOptions<BrandOptions> brandOptions) =>
      {
        BrandStylesheet.Response response = BrandStylesheet.Revalidate
        (
          render(brandOptions.Value),
          httpContext.Request.Headers.IfNoneMatch.ToString()
        );
        httpContext.Response.Headers["Cache-Control"] = BrandStylesheet.Response.CacheControl;
        httpContext.Response.Headers["ETag"] = response.EntityTag;
        if (response.Body is null)
        {
          return Results.StatusCode(response.StatusCode);
        }

        return Results.Text(response.Body, contentType, statusCode: response.StatusCode);
      }
    ).AllowAnonymous();
  }
}
