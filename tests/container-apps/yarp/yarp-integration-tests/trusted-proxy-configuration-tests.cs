#region Purpose
// Configuration semantics of the ingress TrustedProxy section (task 305): absent, empty, network-only
// and malformed values, read from JSON the way appsettings.json is.
#endregion

#region Design
// JSON stores an empty array as a key with a null value, so these tests load JSON rather than an
// in-memory dictionary. Each test builds a minimal host on TestServer with only
// UseTrustedProxyForwardedHeaders and inspects the returned HttpContext. The remote address is set
// on every request: ForwardedHeadersMiddleware skips the trust check when it is null.
#endregion

namespace TrustedProxyConfiguration_;

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Yarp.Server;

/// <summary>
/// Which peers the ingress trusts for X-Forwarded-Proto and X-Forwarded-Host.
/// </summary>
[TestTag("Integration")]
public class TrustedProxy_Given_
{
  private static readonly IPAddress ClusterProxy = IPAddress.Parse("10.1.2.3");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TrustedProxy_Given_>();

  public static async Task AbsentSection_Should_TrustLoopback()
  {
    HttpContext context = await SendForwardedAsync("{}", IPAddress.Loopback);

    context.Request.Scheme.ShouldBe("https");
    context.Request.Host.Value.ShouldBe("public.example.test");
  }

  public static async Task EmptyLists_Should_IgnoreForwardedHeaders()
  {
    HttpContext context = await SendForwardedAsync
    (
      """{ "TrustedProxy": { "KnownProxies": [], "KnownNetworks": [] } }""",
      IPAddress.Loopback
    );

    context.Request.Scheme.ShouldBe("http");
    context.Request.Host.Value.ShouldBe("ingress.internal");
  }

  public static async Task NetworkOnly_Should_ReplaceTheLoopbackDefault()
  {
    const string json = """{ "TrustedProxy": { "KnownNetworks": [ "10.0.0.0/8" ] } }""";

    HttpContext loopback = await SendForwardedAsync(json, IPAddress.Loopback);
    HttpContext clusterProxy = await SendForwardedAsync(json, ClusterProxy);

    loopback.Request.Scheme.ShouldBe("http");
    clusterProxy.Request.Scheme.ShouldBe("https");
    clusterProxy.Request.Host.Value.ShouldBe("public.example.test");
  }

  public static async Task ScalarValue_Should_FailStartup()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      () => SendForwardedAsync("""{ "TrustedProxy": { "KnownProxies": "127.0.0.1" } }""", IPAddress.Loopback)
    );

    exception.Message.ShouldContain("TrustedProxy:KnownProxies");
  }

  public static async Task MalformedNetwork_Should_FailStartup()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      () => SendForwardedAsync("""{ "TrustedProxy": { "KnownNetworks": [ "not-a-cidr" ] } }""", IPAddress.Loopback)
    );

    exception.Message.ShouldContain("not-a-cidr");
  }

  private static async Task<HttpContext> SendForwardedAsync(string json, IPAddress remoteIp)
  {
    WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
    builder.Configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    builder.WebHost.UseTestServer();

    await using WebApplication webApplication = builder.Build();
    webApplication.UseTrustedProxyForwardedHeaders();
    webApplication.Run(_ => Task.CompletedTask);
    await webApplication.StartAsync();

    return await webApplication.GetTestServer().SendAsync
    (
      context =>
      {
        context.Connection.RemoteIpAddress = remoteIp;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("ingress.internal");
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "public.example.test";
      }
    );
  }
}
