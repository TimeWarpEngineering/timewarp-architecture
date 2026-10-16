#region Purpose
// SPA ServiceCollection host for integration tests: TimeWarp.State + generated mediator wired to
// an Aspire resource HttpClient (closed-box), with headless fakes (IJSRuntime) and mock tokens.
#endregion

#region Design
// Not Aspire-constrained beyond the base URL: composition mirrors production SPA registration
// selectively (AddTimeWarpState + ApiServerApiService) rather than Web.Spa.Program wholesale,
// so service-discovery conflicts stay out of the test host. The named api-server HttpClient's
// base address is an Aspire HTTP endpoint (plain http, so the test client needs no dev cert).
// SPA actions such as FetchWeatherForecasts reach api-server through it. Logging is registered because DefaultApiHandler requires ILogger.
// TimeWarp.State.Plus is included so [TrackAction] can resolve ActionTrackingState.
// AddActionCatalog mirrors Web.Spa.Program so IActionCatalog tests see the production roster;
// AddJavaScriptDispatch reuses Web.Spa.Program.AllowJavaScriptDispatch for the same reason (task 278).
// Authorization policies and the WebMCP services mirror the same program so palette-backed tool
// tests resolve IAuthorizationService and WebMcpDispatcher from the per-test scope (task 271).
// This host does not register the production relay; Web.Spa.Program.ConfigureServices does
// (RelayChatClient, no key).
// MockAuthenticationRegistration (Testing + Authentication:UseMock) runs in every flag variant:
// the web-spa mock providers ship whatever the flags, and the catalog-agent tests read the mock
// session's NameIdentifier (task 376).
// The IApiServerApiService factory sits in the api conditional: a generated app with the api
// flag off drops the api-server client. The named HttpClient stays outside that conditional: it
// only names Foundation ServiceNames. Its base address is the ingress when the yarp flag is on.
// With yarp off there is no ingress, so it is api-server itself (which serves api/... routes
// directly), or web-server when api is off too and nothing calls that client.
// Message-bar handlers mutate NotificationState and do not need a rendered FluentUI provider.
// NavigationManager is the headless TestNavigationManager so RouteState.ChangeRoute and
// NotificationState.NavigationListener (both registered here as in the SPA) can be exercised
// without a renderer (task 247).
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using Blazored.LocalStorage;
using FakeItEasy;
using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// Spa test application that uses a started Aspire <see cref="DistributedApplication"/> for
/// the api-server client base URL and builds a SPA ServiceProvider with headless fakes.
/// </summary>
public class AspireSpaTestApplication : ISpaTestApplication
{
#if(yarp)
  private const string BaseUrlResourceName = "ingress";
#elif(api)
  private const string BaseUrlResourceName = "api-server";
#else
  private const string BaseUrlResourceName = "web-server";
#endif

  public IServiceProvider ServiceProvider { get; }

  public AspireSpaTestApplication(DistributedApplication distributedApp)
  {
    ServiceCollection services = new();

    // The ingress proxies to the web and api servers. Without yarp, the client talks to the
    // backend directly.
    using HttpClient baseUrlHttpClient = distributedApp.CreateHttpClient(BaseUrlResourceName, "http");
    string? rawBaseUrl = baseUrlHttpClient.BaseAddress?.ToString();
    if (string.IsNullOrWhiteSpace(rawBaseUrl))
    {
      throw new InvalidOperationException($"{BaseUrlResourceName} HTTP base URL is not configured.");
    }

    string baseUrl = rawBaseUrl.EndsWith('/', StringComparison.Ordinal) ? rawBaseUrl : rawBaseUrl + "/";

    ConfigureServices(services, baseUrl);

    // Dispatch via SpaTestScope (per-test Store/Sender), not root ScopedSender.
    ServiceProvider = services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string baseUrl)
  {
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        [MockAuthenticationDefaults.UseMockKey] = "true"
      })
      .Build();

    if (!MockAuthenticationRegistration.TryAddSpaMockAuthentication(services, configuration, "Testing"))
    {
      throw new InvalidOperationException(
        "SPA integration host requires MockAccessTokenProvider (Testing + Authentication:UseMock).");
    }

    // API handlers take ILogger. The raw ServiceCollection does not register it.
    services.AddLogging();

    // Add only the core services needed for testing (avoid service discovery conflicts)
    services.AddWebSpaGeneratedMediator();
    services.AddTimeWarpState
    (
      options =>
      {
        options.Assemblies =
        [
          typeof(Web.Spa.IAssemblyMarker).Assembly,
          typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
        ];
      }
    );
    services.AddTimeWarpStateBlazor();

    // Same catalog registration as Web.Spa.Program (task 239-002).
    services.AddActionCatalog(typeof(Web.Spa.IAssemblyMarker).Assembly);
    services.AddJavaScriptDispatch(Web.Spa.Program.AllowJavaScriptDispatch);
    services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
    services.AddScoped<PageAgentRoute>();
    services.AddScoped<AgentCallOutcome>();
    services.AddScoped<WebMcpApprovalGate>();
    services.AddScoped<WebMcpDispatcher>();
    services.AddScoped<JsWebMcpModelContext>();
    services.AddScoped<WebMcpPublisher>();

    // Plus notification handlers (LoadPersistentState) are linked into the generated
    // mediator and require IPersistenceService when the pipeline resolves them.
    services.AddTimeWarpStatePersistence();
    // DismissPrivacyNotice and LoadPrivacyNoticeDismissal (task 347) remember the Ask notice in
    // local storage, as Web.Spa.Program registers it.
    services.AddBlazoredLocalStorage();

    // Named client the SPA uses for api-server. Base address is BaseUrlResourceName (see Design).
    // MockAccessTokenProvider attaches Bearer dummy-token.
    services.AddHttpClient(
      TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName,
      client => client.BaseAddress = new Uri(baseUrl));

    // Configure JSON serializer options
    services.Configure<JsonSerializerOptions>(ContractSerializationDefaults.Apply);

#if(api)
    // Register IApiServerApiService (required for handlers that call the API)
    services.AddScoped<IApiServerApiService>(serviceProvider =>
    {
      IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
      IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
      IOptions<JsonSerializerOptions> jsonOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

      return new ApiServerApiService(httpClientFactory, accessTokenProvider, jsonOptions);
    });
#endif
    // Replace JSRuntime with a fake for testing
    IJSRuntime fakeJsRuntime = A.Fake<IJSRuntime>();
    services.AddScoped(_ => fakeJsRuntime);

    // Headless navigation: RouteState.ChangeRoute → NavigationManager → LocationChanged →
    // NotificationState.NavigationListener (same registration shape as web-spa program.cs).
    services.AddScoped<NavigationManager, TestNavigationManager>();
    services.AddScoped<TimeWarp.Architecture.Features.NotificationState.NavigationListener>();
  }
}
