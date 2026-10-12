#region Purpose
// Host-free coverage that IdentitySessionCookieForwardingHandler copies Cookie, mock
// principal, and the circuit's public origin (forwarded proto/host, else Request.Scheme/Host; port
// kept) from HttpContext onto the outgoing self-call's X-Forwarded-Host / X-Forwarded-Proto without
// rewriting HTTP Host (TLS on the https hop must keep validating the URI host).
#endregion

namespace IdentitySessionCookieForwarding_;

using Microsoft.AspNetCore.Http;
using System.Net;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Mail;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Server;

public class Copies_
{
  private const string ForwardedHostHeader = "X-Forwarded-Host";
  private const string ForwardedProtoHeader = "X-Forwarded-Proto";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Copies_>();

  public static async Task Cookie_And_Mock_Principal_Header_From_HttpContext()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Headers.Cookie = $"{IdentitySessionDefaults.CookieName}=ticket";
    string mockPrincipalId = Guid.NewGuid().ToString();
    httpContext.Request.Headers[MockAuthenticationDefaults.MockPrincipalIdHeader] = mockPrincipalId;

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://example.test/api/Users/Current/Profile");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues("Cookie")
      .ShouldContain(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    inner.LastRequest.Headers.GetValues(MockAuthenticationDefaults.MockPrincipalIdHeader)
      .ShouldContain(mockPrincipalId);
  }

  public static async Task Nothing_When_HttpContext_Has_No_Cookie()
  {
    CapturingHandler inner = new();
    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor())
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://example.test/api/Users/Current/Profile");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.Contains("Cookie").ShouldBeFalse();
    inner.LastRequest.Headers.Contains(MockAuthenticationDefaults.MockPrincipalIdHeader).ShouldBeFalse();
    inner.LastRequest.Headers.Contains(ForwardedHostHeader).ShouldBeFalse();
    inner.LastRequest.Headers.Contains(ForwardedProtoHeader).ShouldBeFalse();
  }

  public static async Task Public_Origin_From_HttpContext_Keeps_Port_And_Scheme_Without_Setting_Http_Host()
  {
    // Run mode, web-server reached directly: Request.Scheme/Host are the browser's origin.
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("localhost", 63610);

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://localhost:63611/api/identity/passkey/authenticate");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(ForwardedHostHeader).ShouldBe(["localhost:63610"]);
    inner.LastRequest.Headers.GetValues(ForwardedProtoHeader).ShouldBe(["https"]);
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
  }

  public static async Task Public_Host_Is_Copied_When_HttpContext_Has_No_Cookie()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://localhost:63611/api/identity/passkey/authenticate");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(ForwardedHostHeader)
      .ShouldContain("arch.timewarp.work");
    inner.LastRequest.Headers.GetValues(ForwardedProtoHeader).ShouldBe(["https"]);
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
    inner.LastRequest.Headers.Contains("Cookie").ShouldBeFalse();
  }

  public static async Task Public_Origin_From_Forwarded_Headers_When_Behind_Ingress_Keeps_Port_And_Proto()
  {
    // Behind YARP the circuit request's Host is the destination (web-server) and its scheme is the
    // plain-http hop; the public origin arrives in the ingress-set X-Forwarded-Host/Proto (task 070-008).
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("web-server", 8080);
    httpContext.Request.Headers[ForwardedHostHeader] = "arch.timewarp.work:8443";
    httpContext.Request.Headers[ForwardedProtoHeader] = "https";

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("http://web-server/api/identity/passkey/authenticate");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(ForwardedHostHeader).ShouldBe(["arch.timewarp.work:8443"]);
    inner.LastRequest.Headers.GetValues(ForwardedProtoHeader).ShouldBe(["https"]);
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
  }

  public static async Task Self_Call_Headers_Give_Rp_Id_Without_Port_And_Mail_Origin_With_Port()
  {
    // The self-call hop's two readers: RP-ID selection strips the port, the mail origin keeps it.
    CapturingHandler inner = new();
    DefaultHttpContext circuit = new();
    circuit.Request.Host = new HostString("web-server", 8080);
    circuit.Request.Headers[ForwardedHostHeader] = "arch.timewarp.work:8443";
    circuit.Request.Headers[ForwardedProtoHeader] = "https";

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = circuit })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("http://web-server/api/feedback");

    DefaultHttpContext selfCall = new();
    selfCall.Request.Scheme = "http";
    selfCall.Request.Host = new HostString("web-server");
    selfCall.Request.Headers[ForwardedHostHeader] = inner.LastRequest!.Headers.GetValues(ForwardedHostHeader).ToArray();
    selfCall.Request.Headers[ForwardedProtoHeader] = inner.LastRequest.Headers.GetValues(ForwardedProtoHeader).ToArray();

    HttpRequestHostAccessor.GetPublicHost(selfCall.Request).ShouldBe("arch.timewarp.work");
    HttpAppBaseUrlAccessor.GetRequestOrigin(selfCall.Request).ShouldBe(new Uri("https://arch.timewarp.work:8443/"));
  }

  public static async Task Http_Host_Already_Set_Is_Left_Alone_And_Public_Host_Is_Copied()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    using HttpRequestMessage request = new(HttpMethod.Get, "https://localhost:63611/api/identity/passkey/authenticate");
    request.Headers.Host = "already.set.test";
    await client.SendAsync(request);

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.Host.ShouldBe("already.set.test");
    inner.LastRequest.Headers.GetValues(ForwardedHostHeader)
      .ShouldContain("arch.timewarp.work");
  }

  public static async Task Public_Host_Already_Set_Is_Not_Overwritten()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "https";
    httpContext.Request.Host = new HostString("arch.timewarp.work");

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    using HttpRequestMessage request = new(HttpMethod.Get, "https://localhost:63611/api/identity/passkey/authenticate");
    request.Headers.TryAddWithoutValidation(ForwardedHostHeader, "already.set.test");
    await client.SendAsync(request);

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(ForwardedHostHeader)
      .ShouldContain("already.set.test");
    inner.LastRequest.Headers.GetValues(ForwardedHostHeader)
      .ShouldNotContain("arch.timewarp.work");
    inner.LastRequest.Headers.GetValues(ForwardedProtoHeader).ShouldBe(["https"]);
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
  }

  public static async Task Public_Proto_Already_Set_Is_Not_Overwritten()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Scheme = "http";
    httpContext.Request.Host = new HostString("arch.timewarp.work", 8443);

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    using HttpRequestMessage request = new(HttpMethod.Get, "https://localhost:63611/api/identity/passkey/authenticate");
    request.Headers.TryAddWithoutValidation(ForwardedProtoHeader, "https");
    await client.SendAsync(request);

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(ForwardedProtoHeader).ShouldBe(["https"]);
    inner.LastRequest.Headers.GetValues(ForwardedHostHeader).ShouldBe(["arch.timewarp.work:8443"]);
  }

  private sealed class CapturingHandler : HttpMessageHandler
  {
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      LastRequest = request;
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
  }
}
