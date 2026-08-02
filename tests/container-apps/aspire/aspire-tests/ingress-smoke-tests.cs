#region Purpose
// Request-level smoke THROUGH the YARP ingress (task 117): guards the host-preserving
// http-endpoint forwarding to Web.Server that the 2026-07-22 RemoteCertificateNameMismatch 502
// shipped around — backend health checks alone proved 'backends up', not 'requests flow'.
// Extended for task 107: proves the GENERATED Web.Server /api carve-outs
// (WebServerApiRoutePrefixes) actually reach Web.Server through the ingress — including
// /api/identity (the 104-003 drift that shipped unreachable) and /api/Roles (a live drift the
// hand-maintained list had dropped).
// Framework: Jaribu MTP (task 145-003) — SetupOnce replaces xUnit IClassFixture/IAsyncLifetime.
#endregion

#region Design
// Closed-box Aspire.Hosting.Testing lane (epic 145 two-lane model): full AppHost graph, zero
// DI mocks. SetupOnce starts DistributedApplication once per class; CleanUpOnce disposes.
// Health-gate web→api→ingress then poll ingress reachability (DCP proxy race). Suite-shaped
// under tests/ per hybrid topology policy — not co-located under features/.
#endregion

namespace Aspire.Tests;

/// <summary>
/// Ingress request smoke through the running AppHost (closed-box).
/// </summary>
[TestTag("Integration")]
public class IngressSmoke_Given_
{
  private static DistributedApplication? App;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IngressSmoke_Given_>();

  public static async Task SetupOnce()
  {
    IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
      (
        // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
        // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
        ["--Postgres:UseDataVolume=false"]
      );

    App = await appHost.BuildAsync();
    await App.StartAsync();

    // Requests flow only once the backends AND the ingress are healthy; one 2-minute budget
    // covers all three so a slow backend doesn't false-fail the ingress wait.
    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
    await App.ResourceNotifications.WaitForResourceHealthyAsync("web-server", cts.Token);
    await App.ResourceNotifications.WaitForResourceHealthyAsync("api-server", cts.Token);
    await App.ResourceNotifications.WaitForResourceHealthyAsync("ingress", cts.Token);

    // Healthy != reachable: the ingress reports Healthy when the YARP process starts, but the
    // Aspire DCP proxy takes a moment more to wire through to the replica. Requests issued in that
    // window get an immediate connection EOF ("response ended prematurely"), NOT an HTTP error.
    // Poll until the ingress actually answers so the facts below assert on real responses. Any
    // HTTP status (even a 5xx) proves the proxy is wired — a genuine regression still surfaces as
    // a bad status in the facts, never swallowed here.
    await WaitForIngressReachableAsync(cts.Token);
  }

  public static async Task CleanUpOnce()
  {
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  private static async Task WaitForIngressReachableAsync(CancellationToken cancellationToken)
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");
    HttpRequestException? lastException = null;
    int attempts = 0;

    while (true)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        // Review 117 L1: surface WHY the gate exhausted its budget instead of a bare
        // OperationCanceledException — an unrelated ingress-edge failure should be diagnosable.
        throw new TimeoutException(
          $"Ingress never returned an HTTP response after {attempts} attempts.", lastException);
      }

      try
      {
        using HttpResponseMessage response = await httpClient.GetAsync("/", cancellationToken);
        return;
      }
      catch (HttpRequestException exception)
      {
        // Proxy not wired yet — connection reset before any response. Retry within the budget.
        lastException = exception;
        attempts++;
        await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
      }
    }
  }

  public static async Task RootThroughIngress_Should_ReturnSpaShell()
  {
    // ingress "http" endpoint: TLS terminates at the edge, so the test client needs no cert trust.
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    HttpResponseMessage response = await httpClient.GetAsync("/");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    // The Blazor bootstrap script proves the catch-all reached Web.Server's SPA shell, not just any 200.
    body.ShouldContain("_framework/blazor.web.js");
  }

  public static async Task WebRouteThroughIngressWithForeignHost_Should_ReturnOk()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // This exact request 502'd (RemoteCertificateNameMismatch) when the ingress forwarded web
    // routes over https — the foreign Host moved the cert-name validation target to "smoke.test"
    // and .NET rejected Web.Server's localhost dev cert. Guards the http-endpoint forwarding plus
    // WithTransformUseOriginalHostHeader (task 104-031). Hello is [EndpointAllowAnonymous], so no
    // allowlist/auth setup is needed to reach it.
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    // Web.Server's Hello handler shaped this body — proves the request actually traversed the
    // web backend, not just any 200 from the ingress or a wrong destination.
    body.ShouldContain("Hello, Smoke!");
  }

  public static async Task ApiRouteThroughIngress_Should_ReturnOk()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // Api.Server catch-all over the default https hop — deliberately exercises the internal
    // https forwarding the web routes had to avoid (no original-Host preservation on api routes).
    HttpResponseMessage response = await httpClient.GetAsync("/api/weatherforecast?Days=10");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  public static async Task IdentitySessionThroughIngress_Should_ReachWebServer()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // The exact 104-003 failure: /api/identity/* was unreachable through the ingress and fell to the
    // Api.Server catch-all (404). The generated /api/identity carve-out must now route it to
    // Web.Server. GetCurrentSession is [EndpointAllowAnonymous], so no auth setup is needed; an
    // anonymous session is a valid 200 (IsAuthenticated=false). A 404 here would mean the generated
    // prefix failed to route — the regression this task exists to prevent.
    HttpResponseMessage response = await httpClient.GetAsync("/api/identity/session");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    // Web.Server's GetCurrentSession handler shaped this body — proves the request reached the web
    // backend, not just any 200 from the ingress.
    body.ShouldContain("uthenticated");
  }

  public static async Task RolesThroughIngress_Should_ReachWebServerAndRequireAuth()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // /api/Roles is the LIVE drift the hand-maintained list had dropped: 5 admin endpoints that were
    // falling to the Api.Server catch-all through the ingress. GetRoles is
    // [EndpointAuthorize] on Web.Server, so an unauthenticated request must return 401 — which proves
    // the generated prefix routed it to Web.Server (Api.Server hosts no /api/Roles, so a drift would
    // surface as 404). Asserting 401-not-404 is the bug-fix regression guard.
    HttpResponseMessage response = await httpClient.GetAsync("/api/Roles");

    response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
}

/// <summary>
/// Compile-time proof that generated ingress prefixes include identity and Hello carve-outs.
/// </summary>
[TestTag("Unit")]
public class GeneratedIngressRoutes_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GeneratedIngressRoutes_Given_>();

  public static Task WebServerApiRoutePrefixes_Should_CoverIdentityAndHello()
  {
    // Compile-time proof (no running app): the generator produced the Web.Server /api carve-outs the
    // ingress loops over. api/identity is the 104-003 regression shape; api/Hello is the existing
    // anonymous demo route. Both must be present for the HTTP facts above to be meaningful.
    WebServerApiRoutePrefixes.All.ShouldContain("api/identity");
    WebServerApiRoutePrefixes.All.ShouldContain("api/Hello");
    // /api/GetCurrentUser became [ClientOnlyContract] — it must NOT be a generated ingress prefix.
    WebServerApiRoutePrefixes.All.ShouldNotContain("api/GetCurrentUser");
    return Task.CompletedTask;
  }
}
