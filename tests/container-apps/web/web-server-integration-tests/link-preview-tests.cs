#region Purpose
// Host-free coverage that LinkPreview prefers SiteMetadata.BaseUrl, then the forwarded public
// origin, then the request, and that the page path drops the query.
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

    LinkPreviewValues values = LinkPreview.Resolve(
      Metadata(baseUrl: "https://arch.timewarp.work/app"),
      httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/app/Login");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://arch.timewarp.work/app/images/og-image.png");
    values.PageUrl.Query.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Use_Configured_Base_Url_When_There_Is_No_Request()
  {
    LinkPreviewValues values = LinkPreview.Resolve(Metadata(baseUrl: "https://arch.timewarp.work"), request: null);

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

    LinkPreviewValues values = LinkPreview.Resolve(Metadata(), httpContext.Request);

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

    LinkPreviewValues values = LinkPreview.Resolve(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://localhost:63610/");
    return Task.CompletedTask;
  }

  public static Task Ignore_An_Unknown_Forwarded_Proto()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "javascript";

    LinkPreviewValues values = LinkPreview.Resolve(Metadata(), httpContext.Request);

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

    LinkPreviewValues values = LinkPreview.Resolve(Metadata(), httpContext.Request);

    values.PageUrl.AbsoluteUri.ShouldBe("https://localhost:7000/app/Login");
    values.ImageUrl.AbsoluteUri.ShouldBe("https://localhost:7000/app/images/og-image.png");
    return Task.CompletedTask;
  }

  public static Task Keep_An_Absolute_Image_Url()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 7000);
    SiteMetadata metadata = Metadata();
    metadata.ImagePath = "https://cdn.example/logos/card.png";

    LinkPreviewValues values = LinkPreview.Resolve(metadata, httpContext.Request);

    values.ImageUrl.AbsoluteUri.ShouldBe("https://cdn.example/logos/card.png");
    values.Description.ShouldBe(SiteMetadata.DefaultDescription);
    return Task.CompletedTask;
  }

  public static Task Throw_When_No_Origin_Can_Be_Resolved()
  {
    Should.Throw<InvalidOperationException>(() => LinkPreview.Resolve(Metadata(), request: null));
    return Task.CompletedTask;
  }

  private static SiteMetadata Metadata(string? baseUrl = null) =>
    new()
    {
      BaseUrl = baseUrl is null ? null : new Uri(baseUrl),
    };
}
