#region Purpose
// Host-free coverage that HttpRequestHostAccessor prefers X-TimeWarp-Circuit-Host when present
// and falls back to Request.Host when that internal header is absent.
#endregion

namespace HttpRequestHostAccessor_;

using Microsoft.AspNetCore.Http;
using TimeWarp.Architecture.Services;

public class GetRequestHost_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRequestHost_Should>();

  public static Task Return_Circuit_Host_Header_When_Present()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("localhost", 63611);
    httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader] = "arch.timewarp.work";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Request_Host_When_Circuit_Host_Header_Is_Absent()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work", 443);

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Request_Host_When_Circuit_Host_Header_Is_Empty()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("localhost");
    httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader] = "";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("localhost");

    return Task.CompletedTask;
  }

  public static Task Ignore_X_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Host"] = "evil.test";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Null_When_HttpContext_Is_Missing()
  {
    HttpRequestHostAccessor accessor = new(new HttpContextAccessor());

    accessor.GetRequestHost().ShouldBeNull();

    return Task.CompletedTask;
  }
}
