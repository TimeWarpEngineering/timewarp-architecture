#region Purpose
// Request-level smoke THROUGH the standalone YARP gateway (task 120): generated
// WebServerApiRoutePrefixes carve-outs must reach Web.Server over the Development
// http cluster, including foreign-Host (the 107 https→http / RemoteCertificateNameMismatch path).
// Aspire AppHost ingress remains aspire-tests; this suite boots yarp + web-server in-proc.
#endregion

#region Design
// In-proc HostGraphFactory.CreateWebYarpAsync (C-create): Web :7000/:7001 then Yarp :8443.
// No Aspire, no Api host — the generated prefixes are Web.Server-owned. YarpTestServerApplication
// rewrites the config Web.Server cluster onto http://localhost:7001 so the hop matches Development
// (http, original Host preserved). Suite-shaped under tests/ per hybrid topology policy.
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
    Graph = await HostGraphFactory.CreateWebYarpAsync();
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
    // routes over https — the foreign Host moved cert-name validation to "smoke.test" and .NET
    // rejected Web.Server's localhost dev cert. Guards the Development http-cluster hop plus
    // RequestHeaderOriginalHost (task 104-031). Hello is [EndpointAllowAnonymous].
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await YarpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("Hello, Smoke!");
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

  public static async Task RolesThroughStandaloneYarp_Should_ReachWebServerAndRequireAuth()
  {
    // 401-not-404: GetRoles is [EndpointAuthorize] on Web.Server. Api.Server hosts no /api/Roles,
    // so a missing generated prefix would 404. Asserting 401 proves the carve-out routed to Web.
    HttpResponseMessage response = await YarpClient.GetAsync("/api/Roles");

    response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
}
