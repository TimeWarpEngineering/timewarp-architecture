#region Purpose
// Host-free coverage that IdentitySessionCookieForwardingHandler copies Cookie and mock
// principal headers from HttpContext onto the outgoing loopback request.
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
