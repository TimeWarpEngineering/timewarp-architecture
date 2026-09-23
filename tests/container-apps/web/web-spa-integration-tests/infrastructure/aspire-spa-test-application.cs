#region Purpose
// SPA ServiceCollection host for integration tests: TimeWarp.State + generated mediator wired to
// the Aspire ingress HttpClient (closed-box), with headless fakes (IJSRuntime) and mock tokens.
#endregion

#region Design
// Not Aspire-constrained beyond the base URL: composition mirrors production SPA registration
// selectively (AddTimeWarpState + ApiServerApiService) rather than Web.Spa.Program wholesale,
// so service-discovery conflicts stay out of the test host. The named api-server HttpClient's
// base address is the Aspire ingress HTTP endpoint (TLS terminates at the edge, so the test
// client needs no dev cert). SPA actions such as FetchWeatherForecasts reach api-server
// through YARP. Logging is registered because DefaultApiHandler requires ILogger.
// TimeWarp.State.Plus is included so [TrackAction] can resolve ActionTrackingState.
// MockAuthenticationRegistration (Testing + Authentication:UseMock) and the
// IApiServerApiService factory sit in the api conditional. A generated app with the api
// flag off drops the Services import and the api-server client, so those names do not
// resolve. The named HttpClient stays outside that conditional: it only names Foundation
// ServiceNames. Message-bar handlers mutate NotificationState and do not need a
// rendered FluentUI provider. NavigationManager is the headless TestNavigationManager so
// RouteState.ChangeRoute and NotificationState.NavigationListener (both registered here as
// in the SPA) can be exercised without a renderer (task 247).
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using FakeItEasy;
using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// Spa test application that uses a started Aspire <see cref="DistributedApplication"/> for
/// the ingress base URL and builds a SPA ServiceProvider with headless fakes.
/// </summary>
public class AspireSpaTestApplication : ISpaTestApplication
{
  private const string YarpResourceName = "ingress";

  public IServiceProvider ServiceProvider { get; }

  public AspireSpaTestApplication(DistributedApplication distributedApp)
  {
    ServiceCollection services = new();

    // Get the YARP HTTP client from Aspire - this will proxy to Web and API servers
    using HttpClient yarpHttpClient = distributedApp.CreateHttpClient(YarpResourceName, "http");
    string? rawBaseUrl = yarpHttpClient.BaseAddress?.ToString();
    if (string.IsNullOrWhiteSpace(rawBaseUrl))
    {
      throw new InvalidOperationException("YARP HTTP base URL is not configured.");
    }

    string baseUrl = rawBaseUrl.EndsWith('/') ? rawBaseUrl : rawBaseUrl + "/";

    ConfigureServices(services, baseUrl);

    // Dispatch via SpaTestScope (per-test Store/Sender), not root ScopedSender.
    ServiceProvider = services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string baseUrl)
  {
#if(api)
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
#endif

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

    // Plus notification handlers (LoadPersistentState) are linked into the generated
    // mediator and require IPersistenceService when the pipeline resolves them.
    services.AddScoped<
      TimeWarp.Features.Persistence.IPersistenceService,
      TimeWarp.Features.Persistence.PersistenceService>();

    // Named client the SPA uses for api-server. Base address is ingress, which routes
    // /api/weatherforecast to api-server. MockAccessTokenProvider attaches Bearer dummy-token.
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
