#region Purpose
// Request-level smoke THROUGH the YARP ingress (task 117): guards the host-preserving
// http-endpoint forwarding to Web.Server that the 2026-07-22 RemoteCertificateNameMismatch 502
// shipped around — backend health checks alone proved 'backends up', not 'requests flow'.
#endregion

namespace Aspire.Tests;

public sealed class IngressAppFixture : IAsyncLifetime
{
  public DistributedApplication App { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
    (
      // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
      // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
      ["--Postgres:UseDataVolume=false"]
    );

    App = await appHost.BuildAsync();
    await App.StartAsync();

    // Requests flow only once the backends AND the ingress are healthy; one 2-minute budget
    // covers all three so a slow backend doesn't false-fail the ingress wait.
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
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

  private async Task WaitForIngressReachableAsync(CancellationToken cancellationToken)
  {
    HttpClient httpClient = App.CreateHttpClient("ingress", "http");
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

  public async Task DisposeAsync()
  {
    if (App is not null)
    {
      await App.DisposeAsync();
    }
  }
}

public class IngressSmokeTests : IClassFixture<IngressAppFixture>
{
  private readonly IngressAppFixture Fixture;

  public IngressSmokeTests(IngressAppFixture fixture)
  {
    Fixture = fixture;
  }

  [Fact]
  public async Task GetRootThroughIngressReturnsSpaShell()
  {
    // ingress "http" endpoint: TLS terminates at the edge, so the test client needs no cert trust.
    HttpClient httpClient = Fixture.App.CreateHttpClient("ingress", "http");

    HttpResponseMessage response = await httpClient.GetAsync("/");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    string body = await response.Content.ReadAsStringAsync();
    // The Blazor bootstrap script proves the catch-all reached Web.Server's SPA shell, not just any 200.
    Assert.Contains("_framework/blazor.web.js", body, StringComparison.Ordinal);
  }

  [Fact]
  public async Task GetWebRouteThroughIngressWithForeignHostReturnsOk()
  {
    HttpClient httpClient = Fixture.App.CreateHttpClient("ingress", "http");

    // This exact request 502'd (RemoteCertificateNameMismatch) when the ingress forwarded web
    // routes over https — the foreign Host moved the cert-name validation target to "smoke.test"
    // and .NET rejected Web.Server's localhost dev cert. Guards the http-endpoint forwarding plus
    // WithTransformUseOriginalHostHeader (task 104-031). Hello is [EndpointAllowAnonymous], so no
    // allowlist/auth setup is needed to reach it.
    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await httpClient.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    string body = await response.Content.ReadAsStringAsync();
    // Web.Server's Hello handler shaped this body — proves the request actually traversed the
    // web backend, not just any 200 from the ingress or a wrong destination.
    Assert.Contains("Hello, Smoke!", body, StringComparison.Ordinal);
  }

  [Fact]
  public async Task GetApiRouteThroughIngressReturnsOk()
  {
    HttpClient httpClient = Fixture.App.CreateHttpClient("ingress", "http");

    // Api.Server catch-all over the default https hop — deliberately exercises the internal
    // https forwarding the web routes had to avoid (no original-Host preservation on api routes).
    HttpResponseMessage response = await httpClient.GetAsync("/api/weatherforecast?Days=10");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }
}
