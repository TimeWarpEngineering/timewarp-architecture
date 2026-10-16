#region Purpose
// Browser proof for task 335-002: WebAssembly store actions reach a State DevTools inspector as
// timewarp.state.runtime=wasm spans in a Debug build, and a Release build never asks for the inspector.
#endregion

#region Design
// No AppHost (workers must not start one). A stand-in inspector runs in this process: Kestrel on a
// free loopback port with the real inspector's ingest contract, POST /ingest/spans behind a CORS
// policy that allows only the web host's origin and the Content-Type header. The test sets
// TIMEWARP_STATE_DEVTOOLS_ENDPOINT to that origin, the variable WithTimeWarpStateDevTools sets on
// web-server in run mode, and clears it afterwards. The in-proc web host runs as Development, so
// MapTimeWarpStateDevToolsClientConfig serves GET /_timewarp/state-devtools from that variable.
// The browser context sends no extra headers: a custom header on the cross-origin post would need
// its own CORS allowance, which the real inspector does not grant.
// The WASM exporter is Debug-only (package reference and call), and dev test builds Release. The
// fact reads web-spa's AssemblyConfiguration, not a preprocessor symbol (DEBUG is not a template
// symbol). Debug: a span stamped runtime=wasm and render=interactive with a state action must arrive
// (the inspector's live row; ApplicationState's init is the first). Release: the browser
// must not request the discovery route or any DevTools.Wasm asset, and nothing must arrive. Both
// configurations check the discovery route itself.
#endregion

namespace StateDevToolsWasmPlaywright_;

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

public class StateDevToolsWasm_Given_InspectorEndpoint
{
  private const string EndpointVariable = "TIMEWARP_STATE_DEVTOOLS_ENDPOINT";
  private const string DiscoveryPath = "/_timewarp/state-devtools";
  private const string IngestPath = "/ingest/spans";
  private const string RuntimeAttribute = "timewarp.state.runtime";
  private const string ActionAttribute = "timewarp.state.action";
  private const string RenderAttribute = "timewarp.state.render";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StateDevToolsWasm_Given_InspectorEndpoint>();

  public static async Task Browser_Should_ExportWasmStoreSpans_OnlyInDebug()
  {
    string webOrigin = new Uri(InProcTestPorts.WebHostUrl).GetLeftPart(UriPartial.Authority);
    await using StandInInspector inspector = await StandInInspector.StartAsync(webOrigin);
    string? previous = Environment.GetEnvironmentVariable(EndpointVariable);
    Environment.SetEnvironmentVariable(EndpointVariable, inspector.Origin);
    try
    {
      await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
      PlaywrightBrowserSession session = await PlaywrightBrowserSession.GetAsync();

      await AssertDiscoveryRouteAsync(session.Playwright, inspector.Origin);

      await using IBrowserContext context = await session.Inner.NewContextAsync(new BrowserNewContextOptions
      {
        IgnoreHTTPSErrors = true,
        Locale = "en-US",
      });
      ConcurrentQueue<string> requestUrls = new();
      context.Request += (_, request) => requestUrls.Enqueue(request.Url);

      IPage page = await context.NewPageAsync();
      await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
      await page.WaitForFunctionAsync(
        "() => { const e = document.querySelector('.twe-footer__mode'); return e && e.textContent.trim() !== 'Static'; }",
        null,
        new PageWaitForFunctionOptions { Timeout = 120_000 });

      if (IsWebSpaDebugBuild())
      {
        JsonElement span = await WaitForWasmSpanAsync(inspector, TimeSpan.FromSeconds(30));
        Console.WriteLine($"[335-002] wasm span from the browser: {span}");
        JsonElement attributes = span.GetProperty("Attributes");
        attributes.GetProperty(ActionAttribute).GetString().ShouldNotBeNullOrWhiteSpace();
        attributes.GetProperty(RenderAttribute).GetString().ShouldBe("interactive");
        requestUrls.ShouldContain(url => url.Contains(DiscoveryPath, StringComparison.Ordinal));
      }
      else
      {
        // Give a would-be exporter the same window the Debug branch waits in.
        await Task.Delay(TimeSpan.FromSeconds(5));
        requestUrls.ShouldNotContain(url => url.Contains(DiscoveryPath, StringComparison.Ordinal),
          "a Release WASM bundle asked for the DevTools inspector");
        requestUrls.ShouldNotContain(url => url.Contains("DevTools.Wasm", StringComparison.OrdinalIgnoreCase),
          "a Release WASM bundle loaded TimeWarp.State.DevTools.Wasm");
        inspector.Documents.ShouldBeEmpty("a Release WASM bundle posted spans to the inspector");
      }
    }
    finally
    {
      Environment.SetEnvironmentVariable(EndpointVariable, previous);
    }
  }

  private static bool IsWebSpaDebugBuild() =>
    string.Equals(
      typeof(TimeWarp.Architecture.Web.Spa.Program).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
      "Debug",
      StringComparison.Ordinal);

  private static async Task AssertDiscoveryRouteAsync(IPlaywright playwright, string expectedOrigin)
  {
    await using IAPIRequestContext request = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
    {
      IgnoreHTTPSErrors = true,
    });
    IAPIResponse response = await request.GetAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + DiscoveryPath);
    response.Status.ShouldBe(200, $"GET {DiscoveryPath}");
    using JsonDocument body = JsonDocument.Parse(await response.TextAsync());
    body.RootElement.GetProperty("endpoint").GetString().ShouldBe(expectedOrigin);
  }

  private static async Task<JsonElement> WaitForWasmSpanAsync(StandInInspector inspector, TimeSpan timeout)
  {
    DateTime deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
      foreach (string document in inspector.Documents)
      {
        using JsonDocument parsed = JsonDocument.Parse(document);
        foreach (JsonElement span in parsed.RootElement.GetProperty("Spans").EnumerateArray())
        {
          if (span.TryGetProperty("Attributes", out JsonElement attributes)
            && attributes.TryGetProperty(RuntimeAttribute, out JsonElement runtime)
            && runtime.GetString() == "wasm")
          {
            return span.Clone();
          }
        }
      }

      await Task.Delay(250);
    }

    throw new ShouldAssertException(
      $"No span with {RuntimeAttribute}=wasm reached the inspector within {timeout}. Documents: {inspector.Documents.Count}");
  }

  /// <summary>
  /// Loopback stand-in for the State DevTools inspector's ingest route and its CORS policy.
  /// </summary>
  private sealed class StandInInspector : IAsyncDisposable
  {
    private const string PolicyName = "ingest";
    private readonly WebApplication Application;

    private StandInInspector(WebApplication application, string origin)
    {
      Application = application;
      Origin = origin;
    }

    public string Origin { get; }

    public ConcurrentQueue<string> Documents { get; } = new();

    public static async Task<StandInInspector> StartAsync(string allowedOrigin)
    {
      WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
      builder.WebHost.UseUrls("http://127.0.0.1:0");
      builder.Services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
        .WithOrigins(allowedOrigin)
        .WithMethods(HttpMethods.Post)
        .WithHeaders("Content-Type")));
      WebApplication application = builder.Build();
      application.UseCors();

      StandInInspector? inspector = null;
      application.MapPost(IngestPath, async (HttpRequest request) =>
      {
        using StreamReader reader = new(request.Body);
        inspector!.Documents.Enqueue(await reader.ReadToEndAsync());
        return Results.Accepted();
      }).RequireCors(PolicyName);

      await application.StartAsync();
      string address = application.Services.GetRequiredService<IServer>().Features
        .Get<IServerAddressesFeature>()!.Addresses.Single();
      inspector = new StandInInspector(application, new Uri(address).GetLeftPart(UriPartial.Authority));
      return inspector;
    }

    public async ValueTask DisposeAsync()
    {
      await Application.StopAsync();
      await Application.DisposeAsync();
    }
  }
}
