#region Purpose
// Serves the white-label brand stylesheet (shell tokens) from configuration.
#endregion

#region Design
// White-label shell tokens. Linked from App.razor after tokens.css. Anonymous: the mark
// paints before anyone signs in. Not a static asset, so a configuration edit is the next GET.
// Cache-Control: no-cache plus a strong ETag. If-None-Match uses weak comparison, so the
// strong tag and the same tag with a leading W/ are both 304.
#endregion

namespace TimeWarp.Architecture.Web.Server;
/// <summary>GET the brand stylesheet built from <see cref="BrandOptions"/>.</summary>
internal static class BrandStylesheetEndpoint
{
  /// <summary>Maps <see cref="BrandStylesheet.Path"/>, anonymous.</summary>
  internal static void Map(WebApplication webApplication)
  {
    webApplication.MapGet
    (
      BrandStylesheet.Path,
      (HttpContext httpContext, IOptions<BrandOptions> brandOptions) =>
      {
        BrandStylesheet.Response response = BrandStylesheet.Respond
        (
          brandOptions.Value,
          httpContext.Request.Headers.IfNoneMatch.ToString()
        );
        httpContext.Response.Headers["Cache-Control"] = BrandStylesheet.Response.CacheControl;
        httpContext.Response.Headers["ETag"] = response.EntityTag;
        if (response.Body is null)
        {
          return Results.StatusCode(response.StatusCode);
        }

        return Results.Text(response.Body, "text/css; charset=utf-8", statusCode: response.StatusCode);
      }
    ).AllowAnonymous();
  }
}
