#region Purpose
// Serves pre-authored markdown twins when Accept prefers text/markdown (agent-ready content negotiation).
#endregion

#region Design
// isitagentready / Cloudflare "Markdown for Agents" pattern without edge conversion: key HTML
// routes have static wwwroot twins (e.g. / → /index.md). This middleware rewrites the request
// path to the twin **before** UseRouting so MapStaticAssets matches the .md file and emits
// Content-Type: text/markdown. Browsers send Accept: text/html (no text/markdown) and fall
// through to Blazor unchanged — SPA hosting is untouched.
// Prefer markdown when text/markdown is present with q>0 and its quality is >= text/html's
// (html absent ⇒ 0). That covers Accept: text/markdown and Accept: text/markdown, text/html;
// browsers never list text/markdown so they always get HTML.
// Twin map is explicit (not "append .md") so parameterized SPA routes never invent missing
// files. Missing twin ⇒ next() with original path. GET/HEAD only.
// Vary: Accept is appended on negotiated responses so shared caches store HTML and markdown
// variants separately. Content lives under web-spa/wwwroot (SWA); middleware only rewrites.
#endregion

namespace TimeWarp.Architecture.AgentDiscovery;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

/// <summary>
/// Path rewrite: HTML route → static markdown twin when the client prefers <c>text/markdown</c>.
/// </summary>
public static class MarkdownContentNegotiation
{
  /// <summary>HTML path → wwwroot twin path (leading slash, no trailing slash except root).</summary>
  public static IReadOnlyDictionary<PathString, PathString> TwinPaths { get; } =
    new Dictionary<PathString, PathString>
    {
      [new PathString("/")] = new PathString("/index.md"),
    };

  /// <summary>
  /// Whether the Accept header prefers markdown over HTML for agent content negotiation.
  /// </summary>
  public static bool PrefersMarkdown(IList<MediaTypeHeaderValue>? accept)
  {
    if (accept is null || accept.Count == 0)
    {
      return false;
    }

    double? markdownQuality = null;
    double? htmlQuality = null;

    foreach (MediaTypeHeaderValue mediaType in accept)
    {
      if (!mediaType.MediaType.HasValue)
      {
        continue;
      }

      if (mediaType.MediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase))
      {
        markdownQuality = mediaType.Quality ?? 1.0;
      }
      else if (mediaType.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
      {
        htmlQuality = mediaType.Quality ?? 1.0;
      }
    }

    if (markdownQuality is null || markdownQuality.Value <= 0)
    {
      return false;
    }

    double html = htmlQuality ?? 0;
    return markdownQuality.Value >= html;
  }

  /// <summary>
  /// Parses a raw Accept header value the same way the middleware does.
  /// </summary>
  public static bool PrefersMarkdown(string? acceptHeader)
  {
    if (string.IsNullOrWhiteSpace(acceptHeader))
    {
      return false;
    }

    if (!MediaTypeHeaderValue.TryParseList(new[] { acceptHeader }, out IList<MediaTypeHeaderValue>? parsed))
    {
      return false;
    }

    return PrefersMarkdown(parsed);
  }

  /// <summary>
  /// When the request is GET/HEAD, has a registered twin, and prefers markdown, rewrites
  /// <see cref="HttpRequest.Path"/> to the twin and records <c>Vary: Accept</c>.
  /// </summary>
  public static bool TryRewriteToTwin(HttpContext context)
  {
    HttpRequest request = context.Request;
    if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
    {
      return false;
    }

    if (!TwinPaths.TryGetValue(request.Path, out PathString twinPath))
    {
      return false;
    }

    IList<MediaTypeHeaderValue>? accept = request.GetTypedHeaders().Accept;
    if (!PrefersMarkdown(accept))
    {
      return false;
    }

    request.Path = twinPath;
    context.Response.OnStarting(static state =>
    {
      var httpContext = (HttpContext)state!;
      AppendVaryAccept(httpContext.Response.Headers);
      return Task.CompletedTask;
    }, context);

    return true;
  }

  private static void AppendVaryAccept(IHeaderDictionary headers)
  {
    if (headers.Vary.Count == 0)
    {
      headers.Vary = "Accept";
      return;
    }

    // Preserve existing Vary tokens; add Accept if missing (case-insensitive token match).
    StringValues existing = headers.Vary;
    foreach (string? value in existing)
    {
      if (value is null)
      {
        continue;
      }

      foreach (string token in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
      {
        if (token.Equals("Accept", StringComparison.OrdinalIgnoreCase))
        {
          return;
        }
      }
    }

    headers.Append(HeaderNames.Vary, "Accept");
  }
}

/// <summary>ASP.NET Core middleware entry for <see cref="MarkdownContentNegotiation"/>.</summary>
public sealed class MarkdownContentNegotiationMiddleware
{
  private readonly RequestDelegate Next;

  public MarkdownContentNegotiationMiddleware(RequestDelegate next)
  {
    Next = next;
  }

  public Task InvokeAsync(HttpContext context)
  {
    _ = MarkdownContentNegotiation.TryRewriteToTwin(context);
    return Next(context);
  }
}

/// <summary>Pipeline registration for markdown Accept negotiation.</summary>
public static class MarkdownContentNegotiationExtensions
{
  /// <summary>
  /// Registers markdown twin negotiation. Call **before** <c>UseRouting</c> so the twin path
  /// is what endpoint matching sees.
  /// </summary>
  public static IApplicationBuilder UseMarkdownContentNegotiation(this IApplicationBuilder app) =>
    app.UseMiddleware<MarkdownContentNegotiationMiddleware>();
}
