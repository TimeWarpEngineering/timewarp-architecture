#region Purpose
// Request-level smoke THROUGH the standalone YARP gateway (task 120): generated
// WebServerApiRoutePrefixes carve-outs must reach Web.Server over the Development
// http cluster, including foreign-Host (the 107 https→http / RemoteCertificateNameMismatch path),
// and that the gateway SETS X-Forwarded-Host from the request after trusted-proxy forwarding
// (task 070-008, task 305). Loopback is trusted. An untrusted peer's headers are ignored.
// The exact /api/health and /api tip-alias routes reach Web.Server too (task 439).
// Aspire AppHost ingress remains aspire-tests; this suite boots yarp + web-server in-proc.
#endregion

#region Design
// In-proc HostGraphFactory.CreateWebYarpAsync (C-create): Web then Yarp (InProcTestPorts;
// web base/base+1 then yarp base+1443).
// No Aspire, no Api host — the generated prefixes are Web.Server-owned. YarpTestServerApplication
// rewrites the config Web.Server cluster onto WebHttpUrl so the hop matches Development
// (http; Host is the destination, the public host travels in X-Forwarded-Host — task 070-008).
// UntrustedRemoteIpStartupFilter runs before UseTrustedProxyForwardedHeaders and, only when the
// test sends X-TimeWarp-Test-Remote-Ip, replaces Connection.RemoteIpAddress so the same headers
// can be shown ignored. The filter is not part of the product pipeline.
// Suite-shaped under tests/ per hybrid topology policy.
#endregion

namespace StandaloneYarpIngress_;

/// <summary>
/// Ingress request smoke through the standalone YARP gateway (in-proc).
/// </summary>
[TestTag("Integration")]
public class Smoke_Given_
{
  private static HostGraph? Graph;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Smoke_Given_>();

  public static async Task SetupOnce()
  {
    Graph = await HostGraphFactory.CreateWebYarpAsync
    (
      configureYarp: services => services.AddSingleton<IStartupFilter, UntrustedRemoteIpStartupFilter>()
    );
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  private static HttpClient YarpClient
  {
    get
    {
      Graph.ShouldNotBeNull();
      Graph.Yarp.ShouldNotBeNull();
      return Graph.Yarp.HttpClient;
    }
  }

  public static async Task HelloThroughStandaloneYarp_Should_ReachWebServer()
  {
    HttpResponseMessage response = await YarpClient.GetAsync("/api/Hello?Name=Smoke");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("Hello, Smoke!");
  }

  public static async Task HelloThroughStandaloneYarpWithForeignHost_Should_ReturnOk()
  {
    // This exact request 502'd (RemoteCertificateNameMismatch) when the gateway forwarded web
    // routes over https with the original Host (task 104-031). Since task 070-008 the foreign host
    // travels in X-Forwarded-Host; a foreign Host must still answer. Hello is [EndpointAllowAnonymous].
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await YarpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("Hello, Smoke!");
  }

  public static async Task TrustedForwardedHostThroughStandaloneYarp_Should_SelectThatHost()
  {
    // Loopback is a trusted proxy, so X-Forwarded-Host is applied before Set. not-allowed.example
    // is not an approved RP ID.
    HttpResponseMessage response = await PostStartPasskeyAuthentication(host: null, forwardedHost: "not-allowed.example");

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("Host not allowed");
  }

  public static async Task UntrustedForwardedHostThroughStandaloneYarp_Should_BeOverwritten()
  {
    // The same forged header from a peer outside KnownProxies is ignored. Set writes the Host
    // YARP saw (localhost, allowlisted), so selection answers rp.id "localhost".
    HttpResponseMessage response = await PostStartPasskeyAuthentication(
      host: null,
      forwardedHost: "not-allowed.example",
      remoteIp: "203.0.113.5");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ReadRpId(response)).ShouldBe("localhost");
  }

  public static async Task TrustedForwardedHostNamingAllowedRpId_Should_SelectThatRpId()
  {
    // A foreign Host plus a trusted X-Forwarded-Host of an allowed RP ID selects that RP ID.
    HttpResponseMessage response = await PostStartPasskeyAuthentication(host: "smoke.test", forwardedHost: "localhost");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ReadRpId(response)).ShouldBe("localhost");
  }

  public static async Task UntrustedForeignHostWithForgedAllowedForwardedHost_Should_BeHostNotAllowed()
  {
    // The forged header names an allowed RP ID. An untrusted peer is ignored, so Set writes the
    // foreign Host and selection fails closed.
    HttpResponseMessage response = await PostStartPasskeyAuthentication(
      host: "smoke.test",
      forwardedHost: "localhost",
      remoteIp: "203.0.113.5");

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("Host not allowed");
  }

  public static async Task TrustedForwardedProtoAndHost_Should_RenderHttpsPublicOrigin()
  {
    // Arrives on the http listener. The https scheme in the tags can only have come from the header.
    using HttpClient httpClient = new() { BaseAddress = new Uri(InProcTestPorts.YarpHttpUrl) };
    using HttpRequestMessage request = HtmlRequest();
    request.Headers.Add("X-Forwarded-Proto", "https");
    request.Headers.Add("X-Forwarded-Host", "public.example.test");

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    const string origin = "https://public.example.test/";
    const string image = "https://public.example.test/images/og-image.png";
    body.ShouldContain($"<meta property=\"og:url\" content=\"{origin}\" />");
    body.ShouldContain($"<meta property=\"og:image\" content=\"{image}\" />");
    body.ShouldContain($"<meta name=\"twitter:image\" content=\"{image}\" />");
    body.ShouldContain($"<link rel=\"canonical\" href=\"{origin}\" />");
  }

  public static async Task UntrustedForwardedProtoAndHost_Should_KeepTheSchemeAndHostYarpSaw()
  {
    using HttpClient httpClient = new() { BaseAddress = new Uri(InProcTestPorts.YarpHttpUrl) };
    using HttpRequestMessage request = HtmlRequest();
    request.Headers.Add("X-Forwarded-Proto", "https");
    request.Headers.Add("X-Forwarded-Host", "public.example.test");
    request.Headers.Add(UntrustedRemoteIpStartupFilter.HeaderName, "203.0.113.5");

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    string origin = $"http://localhost:{InProcTestPorts.YarpHttpPort}/";
    body.ShouldNotContain("public.example.test");
    body.ShouldContain($"<meta property=\"og:url\" content=\"{origin}\" />");
    body.ShouldContain($"<link rel=\"canonical\" href=\"{origin}\" />");
  }

  private static async Task<HttpResponseMessage> PostStartPasskeyAuthentication
  (
    string? host,
    string forwardedHost,
    string? remoteIp = null
  )
  {
    // StartPasskeyAuthentication is [EndpointAllowAnonymous]: its first step is RP-ID selection.
    using HttpRequestMessage request = new(HttpMethod.Post, "/api/identity/passkey/authenticate/options")
    {
      Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
    };
    if (host is not null)
    {
      request.Headers.Host = host;
    }

    request.Headers.Add("X-Forwarded-Host", forwardedHost);
    if (remoteIp is not null)
    {
      request.Headers.Add(UntrustedRemoteIpStartupFilter.HeaderName, remoteIp);
    }

    return await YarpClient.SendAsync(request);
  }

  private static HttpRequestMessage HtmlRequest()
  {
    HttpRequestMessage request = new(HttpMethod.Get, "/");
    request.Headers.Accept.ParseAdd("text/html");
    return request;
  }

  private static async Task<string> ReadRpId(HttpResponseMessage response)
  {
    using System.Text.Json.JsonDocument body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    string optionsJson = body.RootElement.GetProperty("optionsJson").GetString()!;
    using System.Text.Json.JsonDocument options = System.Text.Json.JsonDocument.Parse(optionsJson);
    return options.RootElement.GetProperty("rpId").GetString()!;
  }

  public static async Task IdentitySessionThroughStandaloneYarp_Should_ReachWebServer()
  {
    // The 104-003 failure: /api/identity/* fell to the Api.Server catch-all (404). The generated
    // /api/identity carve-out must route it to Web.Server. GetCurrentSession is
    // [EndpointAllowAnonymous]; an anonymous session is a valid 200 (IsAuthenticated=false).
    HttpResponseMessage response = await YarpClient.GetAsync("/api/identity/session");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("uthenticated");
  }

  public static async Task ApiHealthThroughStandaloneYarp_Should_ReachWebServer()
  {
    // Task 439: the OpenAPI document advertises GET /api/health on the ingress origin. Without the
    // exact route it falls to the Api.Server catch-all (404 on the live host).
    HttpResponseMessage response = await YarpClient.GetAsync(TimeWarp.Foundation.Configuration.IngressPaths.WebServerHealthPath);

    response.Content.Headers.ContentType.ShouldNotBeNull();
    response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
    string body = (await response.Content.ReadAsStringAsync()).Trim();
    (body is "Healthy" or "Degraded" or "Unhealthy").ShouldBeTrue(body);
  }

  public static async Task TipAliasThroughStandaloneYarp_Should_MatchWebServerTip()
  {
    // Task 439: separate "/api" and "/api/" routes tied (500 AmbiguousMatchException). Both paths
    // must reach web-server's alias and answer what /api/tip answers there.
    Graph.ShouldNotBeNull();
    Graph.Web.ShouldNotBeNull();
    using HttpResponseMessage direct = await Graph.Web.HttpClient.GetAsync("/api/tip");

    foreach (string path in new[] { "/api", "/api/" })
    {
      using HttpResponseMessage response = await YarpClient.GetAsync(path);
      response.StatusCode.ShouldBe(direct.StatusCode, customMessage: $"{path} through ingress");
    }
  }

  public static async Task RolesThroughStandaloneYarp_Should_ReachWebServerAndRequireAuth()
  {
    // 401-not-404: GetRoles is [EndpointAuthorize] on Web.Server. Api.Server hosts no /api/Roles,
    // so a missing generated prefix would 404. Asserting 401 proves the carve-out routed to Web.
    HttpResponseMessage response = await YarpClient.GetAsync("/api/Roles");

    response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
}

/// <summary>
/// Test-only. Sets <c>Connection.RemoteIpAddress</c> from <see cref="HeaderName"/> before the rest
/// of the pipeline, including trusted-proxy forwarded headers.
/// </summary>
internal sealed class UntrustedRemoteIpStartupFilter : IStartupFilter
{
  internal const string HeaderName = "X-TimeWarp-Test-Remote-Ip";

  public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
  {
    return applicationBuilder =>
    {
      applicationBuilder.Use
      (
        async (context, nextMiddleware) =>
        {
          if (IPAddress.TryParse(context.Request.Headers[HeaderName].ToString(), out IPAddress? remoteIp))
          {
            context.Connection.RemoteIpAddress = remoteIp;
          }

          await nextMiddleware(context);
        }
      );
      next(applicationBuilder);
    };
  }
}
