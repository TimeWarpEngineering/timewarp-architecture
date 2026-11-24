#region Purpose
// A rebranded web server serves /brand.css, /manifest.json, /brand.json, and the document head
// from BrandOptions alone, with no template brand left in them.
#endregion

#region Design
// Task 513 proof for the served documents. The in-proc web host gets a sample brand through
// PostConfigure<BrandOptions> (the same values an app's appsettings section would bind), then the
// tests GET each document and search it for the template brand: TimeWarp, timewarp, the template
// seed #55409c, and the template logo file name. The head check is the <head> element only: the
// body carries code identifiers (TimeWarpPage class names) that are not brand. SiteMetadata's
// description is copy, not BrandOptions, so the sample sets it too (skill tw-brand lists it).
#endregion

namespace BrandDocuments_;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Configuration;

[TestTag("Integration")]
public class Serve_Given_A_Rebranded_Host_
{
  private static readonly string[] TemplateBrand = ["TimeWarp", "timewarp", "#55409c", "timewarp-horizontal.svg"];

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Serve_Given_A_Rebranded_Host_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync(configureWeb: Rebrand);
#else
    Graph = await HostGraphFactory.CreateWebAsync(configureWeb: Rebrand);
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

  public static async Task BrandCss_Should_CarryTheSampleRolesAndNoTemplateBrand()
  {
    string css = await GetTextAsync(BrandStylesheet.Path, "text/css");
    css.ShouldContain("--twe-primary: #0b6e4f;");
    css.ShouldContain("--twe-secondary: #c2571a;");
    css.ShouldContain(BrandStylesheet.DarkSelector + " {");
    css.ShouldContain("--twe-primary-text: #7fd8b0;");
    ShouldCarryNoTemplateBrand(css, BrandStylesheet.Path);
  }

  public static async Task Manifest_Should_NameTheSampleAndItsIcons()
  {
    string manifest = await GetTextAsync(WebManifest.Path, "application/manifest+json");
    using JsonDocument document = JsonDocument.Parse(manifest);
    document.RootElement.GetProperty("name").GetString().ShouldBe("Northwind Traders");
    document.RootElement.GetProperty("theme_color").GetString().ShouldBe("#0b6e4f");
    document.RootElement.GetProperty("background_color").GetString().ShouldBe(BrandOptions.DefaultBackgroundColor);
    manifest.ShouldContain("images/brand/icon-192.png");
    manifest.ShouldContain("images/brand/icon-512.png");
    ShouldCarryNoTemplateBrand(manifest, WebManifest.Path);
  }

  public static async Task BrandJson_Should_BeTheSectionTheSpaBinds()
  {
    string json = await GetTextAsync(BrandDocument.Path, "application/json");
    ConfigurationBuilder builder = new();
    builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
    BrandOptions bound = new();
    builder.Build().GetSection(BrandOptions.SectionName).Bind(bound);
    bound.ProductName.ShouldBe("Northwind Traders");
    bound.EffectiveLogoAlt.ShouldBe("Northwind Traders");
    bound.Tokens.Primary.ShouldBe("#0b6e4f");
    ShouldCarryNoTemplateBrand(json, BrandDocument.Path);
  }

  public static async Task Head_Should_LinkTheSampleIconsAndTitle()
  {
    using HttpRequestMessage request = new(HttpMethod.Get, "/");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    using HttpResponseMessage response = await Web.HttpClient.SendAsync(request);
    string html = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.OK, html);

    int headEnd = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
    headEnd.ShouldBeGreaterThan(0);
    string head = html[..headEnd];
    head.ShouldContain("<title>Northwind Traders</title>");
    head.ShouldContain("href=\"images/brand/favicon.svg\"");
    head.ShouldContain("href=\"images/brand/apple-touch-icon.png\"");
    head.ShouldContain("href=\"manifest.json\"");
    head.ShouldContain("images/brand/og-image.png");
    head.ShouldNotContain("#55409c");
    head.ShouldNotContain("timewarp-horizontal.svg");
    head.ShouldNotContain("TimeWarp Enterprises");

    html.ShouldContain("data-theme-color=\"#0b6e4f\"");
    html.ShouldContain("src=\"images/brand/northwind-logo.svg\"");
  }

  private static void Rebrand(IServiceCollection services)
  {
    services.PostConfigure<BrandOptions>
    (
      brand =>
      {
        brand.ProductName = "Northwind Traders";
        brand.LogoSrc = "images/brand/northwind-logo.svg";
        brand.LogoWidth = 180;
        brand.LogoHeight = 40;
        brand.SeedColor = "#0b6e4f";
        brand.FaviconSrc = "images/brand/favicon.svg";
        brand.AppleTouchIconSrc = "images/brand/apple-touch-icon.png";
        brand.Icon192Src = "images/brand/icon-192.png";
        brand.Icon512Src = "images/brand/icon-512.png";
        brand.OgImageSrc = "images/brand/og-image.png";
        brand.Tokens = new BrandTokenOptions
        {
          Primary = "#0b6e4f",
          PrimaryHover = "#11865f",
          PrimaryActive = "#06492f",
          PrimarySoft = "#a7e3c8",
          Secondary = "#c2571a",
          SecondaryHover = "#d86a2b",
          SecondaryActive = "#8a3a0d",
          SecondaryBright = "#ff9a52",
          Highlight = "#d4a017",
          HighlightSoft = "#f7e3a1",
          Info = "#1f6fb2",
          PrimaryDark = "#7fd8b0",
          SecondaryDark = "#ffb27d",
          HighlightDark = "#f2c94c"
        };
      }
    );
    services.PostConfigure<SiteMetadata>(metadata => metadata.Description = "Wholesale ordering for Northwind Traders.");
  }

  private static async Task<string> GetTextAsync(string path, string mediaType)
  {
    using HttpResponseMessage response = await Web.HttpClient.GetAsync(path);
    string body = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    response.Content.Headers.ContentType.ShouldNotBeNull();
    response.Content.Headers.ContentType.MediaType.ShouldBe(mediaType);
    response.Headers.CacheControl?.NoCache.ShouldBe(true);
    response.Headers.ETag.ShouldNotBeNull();
    return body;
  }

  private static void ShouldCarryNoTemplateBrand(string document, string path)
  {
    foreach (string mark in TemplateBrand)
    {
      document.ShouldNotContain(mark, Case.Sensitive, $"{path} carries the template brand ({mark})");
    }
  }
}
