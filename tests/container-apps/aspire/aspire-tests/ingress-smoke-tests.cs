#region Purpose
// Request-level smoke THROUGH the YARP ingress (task 117): guards the http-endpoint forwarding to
// Web.Server that the 2026-07-22 RemoteCertificateNameMismatch 502 shipped around — backend health
// checks alone proved 'backends up', not 'requests flow'. Task 070-008: proves the ingress SETS
// X-Forwarded-Host (a forged client value never reaches web-server's passkey RP-ID selection).
// Extended for task 107: proves the GENERATED Web.Server /api carve-outs
// (WebServerApiRoutePrefixes) actually reach Web.Server through the ingress — including
// /api/identity (the 104-003 drift that shipped unreachable) and /api/Roles (a live drift the
// hand-maintained list had dropped). Task 270: also proves a first run against an empty
// database logs no Error from web-server.
// Framework: Jaribu MTP (task 145-003) — SetupOnce replaces xUnit IClassFixture/IAsyncLifetime.
#endregion

#region Design
// Closed-box Aspire.Hosting.Testing lane (epic 145 two-lane model): full AppHost graph, zero
// DI mocks. SetupOnce starts DistributedApplication once per class; CleanUpOnce disposes.
// Health-gate web→api→ingress, then wait for web-migrations to reach a terminal state (task
// 155 — the AppHost carries no wait edge there, and this suite's ephemeral Postgres means
// migrations always have real work to do), then poll ingress reachability (DCP proxy race).
// Task 270: because Postgres is ephemeral, every run is a first run against an empty database.
// SetupOnce watches web-server's console from creation (the logger service is keyed by the DCP
// instance id, known only from the first resource notification; the backlog replays to a late
// watcher) so a fact can assert that first boot logged no Error.
// Suite-shaped under tests/ per hybrid topology policy — not co-located under features/.
#endregion

namespace Aspire.Tests;

/// <summary>
/// Ingress request smoke through the running AppHost (closed-box).
/// </summary>
[TestTag("Integration")]
public partial class IngressSmoke_Given_
{
  private static DistributedApplication? App;
  private static readonly ConcurrentQueue<string> WebServerLogLines = new();
  private static CancellationTokenSource? WebServerLogWatch;
  private static Task? WebServerLogWatchTask;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IngressSmoke_Given_>();

  public static async Task SetupOnce()
  {
    IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
      (
        // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
        // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
        // Authentication:UseMock=true is closed-box opt-in only (mock principal header smoke).
        // Local dev run does NOT force mock — AppHost forwards this flag when set explicitly.
        ["--Postgres:UseDataVolume=false", "--Authentication:UseMock=true"]
      );

    App = await appHost.BuildAsync();

    // Task 270: capture web-server's console from its very first line, so the first-run fact
    // below sees the boot seed's output against the ephemeral (empty) database.
    WebServerLogWatch = new CancellationTokenSource();
    WebServerLogWatchTask = WatchWebServerLogsAsync(App, WebServerLogWatch.Token);

    await App.StartAsync();

    // Requests flow only once the backends AND the ingress are healthy; one 2-minute budget
    // covers all the gates below (three health waits, the web-migrations terminal-state wait,
    // and the ingress-reachability poll) so a slow backend doesn't false-fail the ingress wait.
    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
    await App.ResourceNotifications.WaitForResourceHealthyAsync("web-server", cts.Token);
    await App.ResourceNotifications.WaitForResourceHealthyAsync("api-server", cts.Token);
    await App.ResourceNotifications.WaitForResourceHealthyAsync("ingress", cts.Token);

    // Task 155: the AppHost deliberately carries NO wait edge between web-server and
    // web-migrations (a builder-graph WaitFor deadlocks dashboard restarts; WaitForCompletion
    // breaks DCP endpoint wiring for web-server, still so on Aspire 13.6 per task 270 —
    // see AppHost program.cs Design region). That means web-server's own Healthy state above says
    // nothing about migration progress. This suite always boots an EPHEMERAL Postgres
    // (--Postgres:UseDataVolume=false), so RunDatabaseUpdateOnStart has real work to do on every
    // run — unlike a real dev volume, which is already-current after the first boot — so without
    // this wait, DB-backed requests below would race the migration deterministically, not rarely.
    // This is a notification-service POLL from test code (WaitForResourceAsync against the
    // already-running graph), not a builder-graph WaitFor/WaitForCompletion annotation, so it
    // reproduces neither of the two bugs above.
    // TerminalStates includes FailedToStart/Exited, so assert the state reached is Finished —
    // a failed migration should fail HERE with a clear message, not later as confusing
    // schema/auth errors in the DB-backed facts (review 155 round-1 F1).
    string migrationState = await App.ResourceNotifications.WaitForResourceAsync
    (
      "web-migrations",
      KnownResourceStates.TerminalStates,
      cts.Token
    );
    migrationState.ShouldBe
    (
      KnownResourceStates.Finished,
      $"web-migrations ended in '{migrationState}', not Finished — schema is not applied."
    );

    // Backstop only, as of task 058-001: the AppHost now gives the yarp resource an HTTP health
    // check, so the Healthy wait above already means "the DCP host proxy is wired and YARP
    // answered a request" and this poll returns on its first attempt. It predates that check —
    // Healthy used to mean nothing more than "YARP container Running", and requests issued in the
    // gap got an immediate connection EOF ("response ended prematurely"), NOT an HTTP error. Kept
    // as a cheap, explicit reachability assertion for this closed-box edge suite. Any HTTP status
    // (even a 5xx) proves the proxy is wired — a genuine regression still surfaces as a bad
    // status in the facts, never swallowed here.
    await WaitForIngressReachableAsync(cts.Token);
  }

  public static async Task CleanUpOnce()
  {
    if (WebServerLogWatch is not null)
    {
      await WebServerLogWatch.CancelAsync();
      try
      {
        await WebServerLogWatchTask!;
      }
      catch (OperationCanceledException)
      {
        // Expected: the watch only ends by cancellation.
      }

      WebServerLogWatch.Dispose();
      WebServerLogWatch = null;
    }

    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  [GeneratedRegex(@"\x1B\[[0-9;]*m")]
  private static partial Regex AnsiEscape();

  private static async Task WatchWebServerLogsAsync(DistributedApplication app, CancellationToken cancellationToken)
  {
    // DCP keys console logs by instance id (web-server-<suffix>), which exists only once the
    // orchestrator creates the resource; the first web-server notification carries it. The logger
    // service replays its backlog to a new watcher, so subscribing after creation loses nothing.
    string? webServerResourceId = null;
    await foreach (ResourceEvent resourceEvent in app.ResourceNotifications.WatchAsync(cancellationToken))
    {
      if (resourceEvent.Resource.Name == "web-server" && resourceEvent.ResourceId != resourceEvent.Resource.Name)
      {
        webServerResourceId = resourceEvent.ResourceId;
        break;
      }
    }

    ResourceLoggerService resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();
    await foreach (IReadOnlyList<LogLine> batch in resourceLoggerService.WatchAsync(webServerResourceId!).WithCancellation(cancellationToken))
    {
      foreach (LogLine line in batch)
      {
        // Strip ANSI colour codes: the console formatter colours the level ("fail") apart from its colon.
        WebServerLogLines.Enqueue(AnsiEscape().Replace(line.Content, string.Empty));
      }
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

  public static async Task FirstRunOnEmptyDatabase_Should_LogNoErrorFromWebServer()
  {
    // Task 270: this suite's Postgres is ephemeral, so every run is a first run against an empty
    // database, and web-server boots with no wait edge on web-migrations (AppHost Design region).
    // Before the boot seed probed the table, EF logged two Error entries here ("Failed executing
    // DbCommand", "An exception occurred while iterating over the results of a query") for
    // 42P01 on identity.site_settings. Wait until web-server reports it started, so its whole
    // boot (the seed runs before Kestrel) is in the captured console.
    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
    while (!WebServerLogLines.Any(line => line.Contains("Application started", StringComparison.Ordinal)))
    {
      if (WebServerLogWatchTask!.IsCompleted)
      {
        // A faulted or ended watch would otherwise surface only as a bare timeout.
        await WebServerLogWatchTask;
        throw new InvalidOperationException(
          $"web-server log watch ended after {WebServerLogLines.Count} lines without 'Application started'.");
      }

      if (cts.IsCancellationRequested)
      {
        throw new TimeoutException(
          $"web-server log watch saw {WebServerLogLines.Count} lines but no 'Application started':{Environment.NewLine}"
          + string.Join(Environment.NewLine, WebServerLogLines.Take(40)));
      }

      await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken.None);
    }

    string[] errorLines =
    [
      .. WebServerLogLines.Where(line =>
        line.Contains("fail:", StringComparison.Ordinal)
        || line.Contains("crit:", StringComparison.Ordinal)
        || line.Contains("42P01", StringComparison.Ordinal))
    ];
    errorLines.ShouldBeEmpty(string.Join(Environment.NewLine, errorLines));
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
    // routes over https with the original Host (task 104-031). Since task 070-008 the ingress sends
    // the destination as Host and the foreign host travels in X-Forwarded-Host; a foreign Host must
    // still answer on a web route. Hello is [EndpointAllowAnonymous], so no allowlist/auth setup
    // is needed to reach it.
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/Hello?Name=Smoke");
    request.Headers.Host = "smoke.test";

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string body = await response.Content.ReadAsStringAsync();
    // Web.Server's Hello handler shaped this body — proves the request actually traversed the
    // web backend, not just any 200 from the ingress or a wrong destination.
    body.ShouldContain("Hello, Smoke!");
  }

  public static async Task ForgedForwardedHostThroughIngress_Should_BeOverwrittenWithPublicHost()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // Task 070-008: the client's real Host is localhost (allowlisted by default); it also forges
    // X-Forwarded-Host. The ingress must SET (overwrite) X-Forwarded-Host with the Host it received,
    // so web-server selects rp.id "localhost". Had the forged value survived (append, or no
    // transform), selection would see "not-allowed.example" and answer 400 "Host not allowed".
    HttpResponseMessage response = await PostStartPasskeyAuthentication(httpClient, host: null, forwardedHost: "not-allowed.example");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ReadRpId(response)).ShouldBe("localhost");
  }

  public static async Task ForeignHostWithForgedAllowedForwardedHostThroughIngress_Should_BeHostNotAllowed()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // Task 070-008, the other direction: a foreign Host plus a forged X-Forwarded-Host naming an
    // ALLOWED RP ID. The ingress overwrites the forged value with the foreign host, so selection is
    // fail-closed — the forged header never selects (or expands) an RP ID.
    HttpResponseMessage response = await PostStartPasskeyAuthentication(httpClient, host: "smoke.test", forwardedHost: "localhost");

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("Host not allowed");
  }

  private static async Task<HttpResponseMessage> PostStartPasskeyAuthentication(HttpClient httpClient, string? host, string forwardedHost)
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

    return await httpClient.SendAsync(request);
  }

  private static async Task<string> ReadRpId(HttpResponseMessage response)
  {
    using System.Text.Json.JsonDocument body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    string optionsJson = body.RootElement.GetProperty("optionsJson").GetString()!;
    using System.Text.Json.JsonDocument options = System.Text.Json.JsonDocument.Parse(optionsJson);
    return options.RootElement.GetProperty("rpId").GetString()!;
  }

  public static async Task ApiRouteThroughIngress_Should_ReturnOk()
  {
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");

    // Api.Server catch-all over the default https hop — the api routes keep YARP's defaults (no
    // X-Forwarded transform of their own), unlike the plain-HTTP web routes.
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

  public static async Task RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole()
  {
    // Task 145-009 + 147-004: closed-box mock principal authenticates (identity-session-compatible
    // scheme) but effective roles default to Member only — PermissionIds admin policies requires Administrator,
    // so the response is 403 (not 401/404). Proves ingress routes to Web.Server AND role policy runs.
    // In-proc roles-authorization-tests cover the Administrator → 200 path via IPrincipalRoleStore.
    Guid principalId = Guid.NewGuid();
    HttpClient httpClient = App!.CreateHttpClient("ingress", "http");
    // Header name SSOT: MockAuthenticationDefaults.MockPrincipalIdHeader (Web.Spa)
    httpClient.DefaultRequestHeaders.Add("X-TimeWarp-Mock-Principal-Id", principalId.ToString());

    HttpResponseMessage response = await httpClient.GetAsync($"/api/Roles?UserId={principalId:D}");

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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
