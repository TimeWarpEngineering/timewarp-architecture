#region Purpose
// Host-free coverage that HttpRequestHostAccessor honors X-TimeWarp-Circuit-Host only when
// Request.Host is loopback, otherwise reads the public host from X-Forwarded-Host (first value,
// port stripped) and falls back to Request.Host when it is absent.
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

  public static Task Return_X_Forwarded_Host_Over_Request_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Headers["X-Forwarded-Host"] = "arch.timewarp.work";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Strip_Port_From_X_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("web-server");
    httpContext.Request.Headers["X-Forwarded-Host"] = "arch.timewarp.work:63610";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Forged_X_Forwarded_Host_Unchanged_For_Selection_To_Reject()
  {
    // The accessor reports; WebAuthnRelyingPartySelection decides. A forged, unapproved value is not
    // replaced by Request.Host — selection rejects it as "host not allowed" (fail-closed).
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Host"] = "evil.test";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("evil.test");

    return Task.CompletedTask;
  }

  public static Task Return_First_Entry_Of_Comma_Separated_X_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("web-server");
    httpContext.Request.Headers["X-Forwarded-Host"] = "arch.timewarp.work:443, evil.test";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_First_Value_Of_Repeated_X_Forwarded_Host()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("web-server");
    httpContext.Request.Headers["X-Forwarded-Host"] = new Microsoft.Extensions.Primitives.StringValues(["arch.timewarp.work", "evil.test"]);

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Request_Host_When_X_Forwarded_Host_Is_Empty()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers["X-Forwarded-Host"] = "";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Circuit_Host_Header_Over_X_Forwarded_Host_On_Loopback()
  {
    // The loopback rule is unchanged: on a loopback Host the internal circuit header wins.
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("localhost", 63611);
    httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader] = "arch.timewarp.work";
    httpContext.Request.Headers["X-Forwarded-Host"] = "evil.test";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Ignore_Circuit_Host_Header_When_Request_Host_Is_Not_Loopback()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");
    httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader] = "evil.test";

    HttpRequestHostAccessor accessor = new(new HttpContextAccessor { HttpContext = httpContext });

    accessor.GetRequestHost().ShouldBe("arch.timewarp.work");

    return Task.CompletedTask;
  }

  public static Task Return_Circuit_Host_Header_When_Request_Host_Is_Loopback_Ip()
  {
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("127.0.0.1");
    httpContext.Request.Headers[MockAuthenticationDefaults.CircuitHostHeader] = "arch.timewarp.work";

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
