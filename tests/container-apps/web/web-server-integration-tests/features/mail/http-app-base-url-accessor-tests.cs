#region Purpose
// Host-free coverage that HttpAppBaseUrlAccessor prefers Mail:PublicBaseUrl, then the forwarded
// public origin (first X-Forwarded-Proto and X-Forwarded-Host values, port kept), then the request.
#endregion

namespace HttpAppBaseUrlAccessor_;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Mail;

public class GetBaseUrl_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetBaseUrl_Should>();

  public static Task Return_Configured_Public_Base_Url_First()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "evil.test";

    Accessor(httpContext, publicBaseUrl: "https://arch.timewarp.work/app")
      .GetBaseUrl()!.AbsoluteUri.ShouldBe("https://arch.timewarp.work/app");

    return Task.CompletedTask;
  }

  public static Task Return_Configured_Public_Base_Url_Without_A_Request()
  {
    Accessor(httpContext: null, publicBaseUrl: "https://arch.timewarp.work")
      .GetBaseUrl()!.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");

    return Task.CompletedTask;
  }

  public static Task Return_Forwarded_Origin_Over_Internal_Hop()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "arch.timewarp.work";

    Accessor(httpContext).GetBaseUrl()!.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");

    return Task.CompletedTask;
  }

  public static Task Keep_The_Port_From_X_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
    httpContext.Request.Headers["X-Forwarded-Host"] = "localhost:63610, evil.test";

    Accessor(httpContext).GetBaseUrl()!.AbsoluteUri.ShouldBe("https://localhost:63610/");

    return Task.CompletedTask;
  }

  public static Task Ignore_An_Unknown_Forwarded_Proto()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Proto"] = "javascript";

    Accessor(httpContext).GetBaseUrl()!.AbsoluteUri.ShouldBe("https://arch.timewarp.work/");

    return Task.CompletedTask;
  }

  public static Task Fall_Back_To_Request_Scheme_Host_And_Path_Base()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 7000);
    httpContext.Request.PathBase = "/app";

    Accessor(httpContext).GetBaseUrl()!.AbsoluteUri.ShouldBe("https://localhost:7000/app");

    return Task.CompletedTask;
  }

  public static Task Return_Null_When_HttpContext_Is_Missing()
  {
    Accessor(httpContext: null).GetBaseUrl().ShouldBeNull();

    return Task.CompletedTask;
  }

  private static HttpAppBaseUrlAccessor Accessor(HttpContext? httpContext, string? publicBaseUrl = null) =>
    new(
      new HttpContextAccessor { HttpContext = httpContext },
      Options.Create(new MailOptions { PublicBaseUrl = publicBaseUrl is null ? null : new Uri(publicBaseUrl) }));
}
