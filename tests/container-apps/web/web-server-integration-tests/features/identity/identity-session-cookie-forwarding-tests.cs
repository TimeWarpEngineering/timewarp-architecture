#region Purpose
// Host-free coverage that IdentitySessionCookieForwardingHandler copies Cookie, mock
// principal, and circuit host from HttpContext onto the outgoing loopback request without
// rewriting HTTP Host (HTTPS TLS must keep validating the URI host).
#endregion

namespace IdentitySessionCookieForwarding_;

using System.Net;
using Microsoft.AspNetCore.Http;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Server;

public class Copies_
{
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
    inner.LastRequest.Headers.Contains(MockAuthenticationDefaults.CircuitHostHeader).ShouldBeFalse();
  }

  public static async Task Circuit_Host_From_HttpContext_Is_Copied_Port_Stripped_Without_Setting_Http_Host()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work", 443);

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://localhost:63611/api/identity/passkey/authenticate");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(MockAuthenticationDefaults.CircuitHostHeader)
      .ShouldContain("arch.timewarp.work");
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
  }

  public static async Task Circuit_Host_Is_Copied_When_HttpContext_Has_No_Cookie()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    await client.GetAsync("https://localhost:63611/api/identity/passkey/authenticate");

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(MockAuthenticationDefaults.CircuitHostHeader)
      .ShouldContain("arch.timewarp.work");
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
    inner.LastRequest.Headers.Contains("Cookie").ShouldBeFalse();
  }

  public static async Task Http_Host_Already_Set_Is_Left_Alone_And_Circuit_Host_Is_Copied()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
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
    inner.LastRequest.Headers.GetValues(MockAuthenticationDefaults.CircuitHostHeader)
      .ShouldContain("arch.timewarp.work");
  }

  public static async Task Circuit_Host_Already_Set_Is_Not_Overwritten()
  {
    CapturingHandler inner = new();
    DefaultHttpContext httpContext = new();
    httpContext.Request.Host = new HostString("arch.timewarp.work");

    IdentitySessionCookieForwardingHandler handler = new(new HttpContextAccessor { HttpContext = httpContext })
    {
      InnerHandler = inner
    };

    using HttpClient client = new(handler);
    using HttpRequestMessage request = new(HttpMethod.Get, "https://localhost:63611/api/identity/passkey/authenticate");
    request.Headers.TryAddWithoutValidation(MockAuthenticationDefaults.CircuitHostHeader, "already.set.test");
    await client.SendAsync(request);

    inner.LastRequest.ShouldNotBeNull();
    inner.LastRequest!.Headers.GetValues(MockAuthenticationDefaults.CircuitHostHeader)
      .ShouldContain("already.set.test");
    inner.LastRequest.Headers.GetValues(MockAuthenticationDefaults.CircuitHostHeader)
      .ShouldNotContain("arch.timewarp.work");
    string.IsNullOrEmpty(inner.LastRequest.Headers.Host).ShouldBeTrue();
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
