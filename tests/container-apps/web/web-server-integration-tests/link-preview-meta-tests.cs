#region Purpose
// The running web server's first HTML response carries Open Graph and Twitter card tags, and the
// image those tags point at is a PNG.
#endregion

#region Design
// Hits GET / on the in-proc web host (not a source-string match). The test appsettings.json sets
// SiteMetadata:Title to a value that is not the C# default, so a section that fails to bind cannot
// pass. Description stays the template default. A second request sends X-Forwarded-Proto and
// X-Forwarded-Host and expects those in og:url, the production path behind YARP.
#endregion

namespace LinkPreviewMeta_;

using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using TimeWarp.Architecture.Configuration;

[TestTag("Integration")]
public class Serve_Given_
{
  private const string TestTitle = "Integration Test Site";

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Serve_Given_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Home_Should_RenderLinkPreviewTagsAndServeTheImage()
  {
    string html = await GetHtmlAsync("/");
    string pageUrl = RequireMeta(html, "property", "og:url");
    string imageUrl = RequireMeta(html, "property", "og:image");

    html.ShouldContain("<title>TimeWarp.Architecture</title>");
    RequireMeta(html, "name", "description").ShouldBe(SiteMetadata.DefaultDescription);
    RequireMeta(html, "property", "og:site_name").ShouldBe(SiteMetadata.DefaultSiteName);
    RequireMeta(html, "property", "og:title").ShouldBe(TestTitle);
    RequireMeta(html, "property", "og:description").ShouldBe(SiteMetadata.DefaultDescription);
    RequireMeta(html, "property", "og:type").ShouldBe(SiteMetadata.OpenGraphType);
    RequireMeta(html, "property", "og:image:width").ShouldBe(SiteMetadata.DefaultImageWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
    RequireMeta(html, "property", "og:image:height").ShouldBe(SiteMetadata.DefaultImageHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
    RequireMeta(html, "property", "og:image:type").ShouldBe(SiteMetadata.DefaultImageType);
    RequireMeta(html, "property", "og:image:alt").ShouldBe(SiteMetadata.DefaultImageAlt);
    RequireMeta(html, "name", "twitter:card").ShouldBe(SiteMetadata.TwitterCard);
    RequireMeta(html, "name", "twitter:title").ShouldBe(TestTitle);
    RequireMeta(html, "name", "twitter:description").ShouldBe(SiteMetadata.DefaultDescription);
    RequireMeta(html, "name", "twitter:image").ShouldBe(imageUrl);
    RequireLink(html, "canonical").ShouldBe(pageUrl);

    ShouldBeAbsoluteHttp(pageUrl);
    ShouldBeAbsoluteHttp(imageUrl);
    string origin = Origin();
    pageUrl.ShouldBe(origin + "/");
    imageUrl.ShouldBe(origin + "/images/og-image.png");

    using HttpResponseMessage image = await Web.HttpClient.GetAsync(imageUrl);
    image.StatusCode.ShouldBe(HttpStatusCode.OK);
    image.Content.Headers.ContentType.ShouldNotBeNull();
    image.Content.Headers.ContentType.MediaType.ShouldBe("image/png");
    byte[] bytes = await image.Content.ReadAsByteArrayAsync();
    bytes.Length.ShouldBeLessThan(300 * 1024);
    ReadPngSize(bytes).ShouldBe((SiteMetadata.DefaultImageWidth, SiteMetadata.DefaultImageHeight));
  }

  public static async Task ForwardedHost_Should_ReplaceTheInternalHop()
  {
    string html = await GetHtmlAsync(
      "/",
      request =>
      {
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "arch.timewarp.work");
      });

    RequireMeta(html, "property", "og:url").ShouldBe("https://arch.timewarp.work/");
    RequireMeta(html, "property", "og:image").ShouldBe("https://arch.timewarp.work/images/og-image.png");
    RequireLink(html, "canonical").ShouldBe("https://arch.timewarp.work/");
  }

  public static async Task QueryString_Should_StayOffTheCanonicalUrl()
  {
    string html = await GetHtmlAsync("/?utm=discord");
    string pageUrl = RequireMeta(html, "property", "og:url");
    pageUrl.ShouldBe(Origin() + "/");
    pageUrl.ShouldNotContain("utm");
    RequireLink(html, "canonical").ShouldBe(pageUrl);
  }

  public static async Task OtherRoute_Should_UseThatPath()
  {
    string html = await GetHtmlAsync("/Login");
    string origin = Origin();
    RequireMeta(html, "property", "og:url").ShouldBe(origin + "/Login");
    RequireMeta(html, "property", "og:image").ShouldBe(origin + "/images/og-image.png");
  }

  private static string Origin() =>
    Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

  private static async Task<string> GetHtmlAsync(string path, Action<HttpRequestMessage>? configure = null)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    configure?.Invoke(request);
    using HttpResponseMessage response = await Web.HttpClient.SendAsync(request);
    string html = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.OK, html);
    return html;
  }

  private static string RequireMeta(string html, string attributeName, string attributeValue)
  {
    string? content = FindAttribute(html, "meta", attributeName, attributeValue, "content");
    content.ShouldNotBeNullOrWhiteSpace($"<meta {attributeName}=\"{attributeValue}\">");
    return System.Net.WebUtility.HtmlDecode(content);
  }

  private static string RequireLink(string html, string rel)
  {
    string? href = FindAttribute(html, "link", "rel", rel, "href");
    href.ShouldNotBeNullOrWhiteSpace($"<link rel=\"{rel}\">");
    return System.Net.WebUtility.HtmlDecode(href);
  }

  private static string? FindAttribute(
    string html,
    string element,
    string keyName,
    string keyValue,
    string valueName)
  {
    string open = "<" + element;
    int index = 0;
    while (index < html.Length)
    {
      int start = html.IndexOf(open, index, StringComparison.OrdinalIgnoreCase);
      if (start < 0)
      {
        return null;
      }

      int end = html.IndexOf('>', start, StringComparison.Ordinal);
      if (end < 0)
      {
        return null;
      }

      Dictionary<string, string> attributes = Attributes(html[start..(end + 1)]);
      if (
        attributes.TryGetValue(keyName, out string? actual)
        && string.Equals(actual, keyValue, StringComparison.OrdinalIgnoreCase)
        && attributes.TryGetValue(valueName, out string? value))
      {
        return value;
      }

      index = end + 1;
    }

    return null;
  }

  private static Dictionary<string, string> Attributes(string tag)
  {
    Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase);
    int index = tag.IndexOf(' ', StringComparison.Ordinal);
    if (index < 0)
    {
      return attributes;
    }

    while (index < tag.Length)
    {
      while (index < tag.Length && char.IsWhiteSpace(tag[index]))
      {
        index++;
      }

      if (index >= tag.Length || tag[index] is '>' or '/')
      {
        break;
      }

      int nameStart = index;
      while (index < tag.Length && tag[index] is not ('=' or ' ' or '>' or '/'))
      {
        index++;
      }

      string attributeName = tag[nameStart..index];
      while (index < tag.Length && char.IsWhiteSpace(tag[index]))
      {
        index++;
      }

      if (index >= tag.Length || tag[index] != '=')
      {
        continue;
      }

      index++;
      while (index < tag.Length && char.IsWhiteSpace(tag[index]))
      {
        index++;
      }

      if (index >= tag.Length || tag[index] is not ('"' or '\''))
      {
        break;
      }

      char quote = tag[index];
      index++;
      int valueStart = index;
      while (index < tag.Length && tag[index] != quote)
      {
        index++;
      }

      attributes[attributeName] = tag[valueStart..index];
      if (index < tag.Length)
      {
        index++;
      }
    }

    return attributes;
  }

  private static void ShouldBeAbsoluteHttp(string value)
  {
    Uri.TryCreate(value, UriKind.Absolute, out Uri? uri).ShouldBeTrue(value);
    uri.ShouldNotBeNull();
    uri.Scheme.ShouldBeOneOf(Uri.UriSchemeHttp, Uri.UriSchemeHttps);
  }

  private static (int Width, int Height) ReadPngSize(byte[] bytes)
  {
    bytes.Length.ShouldBeGreaterThan(24);
    bytes[0].ShouldBe((byte)0x89);
    bytes[1].ShouldBe((byte)0x50);
    bytes[2].ShouldBe((byte)0x4E);
    bytes[3].ShouldBe((byte)0x47);
    int width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
    int height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
    return (width, height);
  }
}
