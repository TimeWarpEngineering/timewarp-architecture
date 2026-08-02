#region Purpose
// SPA ServiceCollection host for integration tests: TimeWarp.State + mediator wired to the
// Aspire ingress HttpClient (closed-box), with headless fakes (IJSRuntime, IAccessTokenProvider)
// and toast-handler removal so ExceptionNotification does not need FluentToastProvider.
#endregion

#region Design
// Not Aspire-constrained beyond the base URL: composition mirrors production SPA registration
// selectively (AddTimeWarpState + ApiServerApiService) rather than Web.Spa.Program wholesale,
// so service-discovery conflicts stay out of the test host. ExceptionNotification removal was
// previously on the deleted SpaTestApplication path; kept here so rollback / headless tests
// match that semantics (task 145-006).
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using FakeItEasy;
using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;
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
    var services = new ServiceCollection();

    // Get the YARP HTTP client from Aspire - this will proxy to Web and API servers
    HttpClient yarpHttpClient = distributedApp.CreateHttpClient(YarpResourceName);
    string baseUrl = yarpHttpClient.BaseAddress?.ToString()
      ?? throw new InvalidOperationException("YARP base URL not configured");

    ConfigureServices(services, baseUrl);

    // Dispatch via SpaTestScope (per-test Store/Sender), not root ScopedSender.
    ServiceProvider = services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string baseUrl)
  {
    IConfiguration configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", optional: true)
      .Build();

    // Register FluentUI services (required by toast notifications when still present)
    services.AddFluentUIComponents();

    // Add only the core services needed for testing (avoid service discovery conflicts)
    services.AddTimeWarpState
    (
      options =>
      {
        options.Assemblies =
        [
          typeof(Web.Spa.IAssemblyMarker).Assembly
        ];
      }
    );

    // Add HttpClient pointing to the YARP gateway from Aspire
    services.AddHttpClient(
      TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName,
      c => c.BaseAddress = new Uri(baseUrl));

    // Configure JSON serializer options
    services.Configure<JsonSerializerOptions>(ContractSerializationDefaults.Apply);

    // Register IAccessTokenProvider (required for API service)
    IAccessTokenProvider fakeAccessTokenProvider = A.Fake<IAccessTokenProvider>();
    services.AddScoped(_ => fakeAccessTokenProvider);

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

    // The ExceptionNotificationHandler shows a FluentUI toast (INotificationService), which needs a
    // rendered <FluentToastProvider> component not present in headless tests — it throws
    // FluentServiceProviderException. Toasts are a UI concern, so drop that handler here (mirrors
    // the IJSRuntime fake above); error-path state tests still exercise rollback via
    // StateTransactionBehavior.
    services.RemoveAll<TimeWarp.Mediator.INotificationHandler<TimeWarp.Features.StateTransactions.ExceptionNotification>>();
  }
}
