#region Purpose
// Request-level smoke THROUGH the standalone YARP gateway (task 120): generated
// WebServerApiRoutePrefixes carve-outs must reach Web.Server over the Development
// http cluster, including foreign-Host (the 107 https→http / RemoteCertificateNameMismatch path),
// and that the gateway SETS X-Forwarded-Host so a forged client value never reaches passkey RP-ID
// selection (task 070-008).
// Aspire AppHost ingress remains aspire-tests; this suite boots yarp + web-server in-proc.
#endregion

#region Design
// In-proc HostGraphFactory.CreateWebYarpAsync (C-create): Web then Yarp (InProcTestPorts;
// defaults :7000/:7001 then :8443).
// No Aspire, no Api host — the generated prefixes are Web.Server-owned. YarpTestServerApplication
// rewrites the config Web.Server cluster onto WebHttpUrl so the hop matches Development
// (http; Host is the destination, the public host travels in X-Forwarded-Host — task 070-008). Suite-shaped under tests/ per hybrid topology policy.
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
    // routes over https with the original Host (task 104-031). Since task 070-008 the foreign host
    // travels in X-Forwarded-Host; a foreign Host must still answer. Hello is [EndpointAllowAnonymous].
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await YarpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("Hello, Smoke!");
  }

  public static async Task ForgedForwardedHostThroughStandaloneYarp_Should_BeOverwritten()
  {
    // Task 070-008: the real Host is localhost (allowlisted); the forged X-Forwarded-Host is not.
    // "X-Forwarded": "Set" overwrites it, so selection still answers rp.id "localhost".
    HttpResponseMessage response = await PostStartPasskeyAuthentication(host: null, forwardedHost: "not-allowed.example");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ReadRpId(response)).ShouldBe("localhost");
  }

  public static async Task ForeignHostWithForgedAllowedForwardedHostThroughStandaloneYarp_Should_BeHostNotAllowed()
  {
    // The forged header names an allowed RP ID; the gateway replaces it with the foreign Host, so
    // selection fails closed.
    HttpResponseMessage response = await PostStartPasskeyAuthentication(host: "smoke.test", forwardedHost: "localhost");

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("Host not allowed");
  }

  private static async Task<HttpResponseMessage> PostStartPasskeyAuthentication(string? host, string forwardedHost)
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

    return await YarpClient.SendAsync(request);
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

  public static async Task RolesThroughStandaloneYarp_Should_ReachWebServerAndRequireAuth()
  {
    // 401-not-404: GetRoles is [EndpointAuthorize] on Web.Server. Api.Server hosts no /api/Roles,
    // so a missing generated prefix would 404. Asserting 401 proves the carve-out routed to Web.
    HttpResponseMessage response = await YarpClient.GetAsync("/api/Roles");

    response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
}
