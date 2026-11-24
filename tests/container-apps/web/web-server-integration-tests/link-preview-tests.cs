#region Purpose
// Host-free coverage that LinkPreview prefers SiteMetadata.BaseUrl, then the forwarded public
// origin, then the request, that the page path drops the query, that an unresolvable origin
// yields no tags instead of throwing, and that the image, site name, title, and image alt come
// from BrandOptions unless SiteMetadata names them (task 513).
#endregion

namespace LinkPreview_;

using Microsoft.AspNetCore.Http;
using TimeWarp.Architecture.Configuration;

public class Resolve_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Resolve_Should>();

  public static Task Prefer_Configured_Base_Url_And_Keep_The_Request_Path()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Path = "/Login";
    httpContext.Request.QueryString = new QueryString("?utm=1");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "evil.test";

    LinkPreviewValues values = Resolved(
      Metadata(baseUrl: "https://arch.timewarp.work/app"),
      httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/app/Login");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/app/images/og-image.png");
    values.PageUrl.Query.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Use_Configured_Base_Url_When_There_Is_No_Request()
  {
    LinkPreviewValues values = Resolved(Metadata(baseUrl: "https://arch.timewarp.work"), request: null);

    values.PageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/images/og-image.png");
    return Task.CompletedTask;
  }

  public static Task Prefer_Forwarded_Origin_Over_The_Internal_Hop()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "arch.timewarp.work";

    LinkPreviewValues values = Resolved(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/images/og-image.png");
    return Task.CompletedTask;
  }

  public static Task Keep_The_Port_On_The_First_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "localhost:63610, evil.test";

    LinkPreviewValues values = Resolved(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://localhost:63610/");
    return Task.CompletedTask;
  }

  public static Task Ignore_An_Unknown_Forwarded_Proto()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "javascript";

    LinkPreviewValues values = Resolved(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");
    return Task.CompletedTask;
  }

  public static Task Include_Path_Base_And_Drop_The_Query()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 7000);
    httpContext.Request.PathBase = "/app";
    httpContext.Request.Path = "/Login";
    httpContext.Request.QueryString = new QueryString("?utm=discord");

    LinkPreviewValues values = Resolved(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://localhost:7000/app/Login");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://localhost:7000/app/images/og-image.png");
    return Task.CompletedTask;
  }

  public static Task Keep_An_Absolute_Image_Url()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 7000);
    BrandOptions brand = new() { OgImageSrc = "https://cdn.example/logos/card.png" };

    LinkPreviewValues values = Resolved(Metadata(), httpContext.Request, brand);

    values.ImageUrl.AbsoluteUri.ShouldBe("https://cdn.example/logos/card.png");
    values.Description.ShouldBe(SiteMetadata.DefaultDescription);
    return Task.CompletedTask;
  }

  public static Task Take_The_Names_From_The_Brand_Unless_Configured()
  {
    BrandOptions brand = new() { ProductName = "Northwind", LogoAlt = "Northwind mark" };
    LinkPreviewValues values = Resolved(Metadata(baseUrl: "https://northwind.example"), request: null, brand);
    values.SiteName.ShouldBe("Northwind");
    values.Title.ShouldBe("Northwind");
    values.ImageAlt.ShouldBe("Northwind mark");

    SiteMetadata named = Metadata(baseUrl: "https://northwind.example");
    named.SiteName = "Northwind Site";
    named.Title = "Northwind Home";
    named.ImageAlt = "Card";
    LinkPreviewValues configured = Resolved(named, request: null, brand);
    configured.SiteName.ShouldBe("Northwind Site");
    configured.Title.ShouldBe("Northwind Home");
    configured.ImageAlt.ShouldBe("Card");
    return Task.CompletedTask;
  }

  public static Task Return_Null_When_No_Origin_Can_Be_Resolved()
  {
    LinkPreview.Resolve(Metadata(), new BrandOptions(), request: null).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Return_Null_For_A_Malformed_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 7000);
    httpContext.Request.Headers["X-Forwarded-Host"] = "a b";

    LinkPreview.Resolve(Metadata(), new BrandOptions(), httpContext.Request).ShouldBeNull();
    return Task.CompletedTask;
  }

  private static LinkPreviewValues Resolved(SiteMetadata metadata, HttpRequest? request, BrandOptions? brand = null)
  {
    LinkPreviewValues? values = LinkPreview.Resolve(metadata, brand ?? new BrandOptions(), request);
    values.ShouldNotBeNull();
    return values;
  }

  private static SiteMetadata Metadata(string? baseUrl = null) =>
    new()
    {
      BaseUrl = baseUrl is null ? null : new Uri(baseUrl),
    };
}
