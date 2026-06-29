namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using FakeItEasy;
using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;
using Microsoft.JSInterop;
using AspireConstants = TimeWarp.Architecture.Aspire.Constants;

/// <summary>
/// Spa test application that uses Aspire DistributedApplication for service orchestration
/// </summary>
public class AspireSpaTestApplication : ISpaTestApplication
{
  private const string YarpResourceName = "ingress";

  private readonly ISender ScopedSender;
  public IServiceProvider ServiceProvider { get; }

  public AspireSpaTestApplication(Task<DistributedApplication> distributedAppTask)
  {
    // Await the distributed application
    DistributedApplication distributedApp = distributedAppTask.Result;

    var services = new ServiceCollection();

    // Get the YARP HTTP client from Aspire - this will proxy to Web and API servers
    // Aspire handles starting all dependent services automatically
    HttpClient yarpHttpClient = distributedApp.CreateHttpClient(YarpResourceName);
    string baseUrl = yarpHttpClient.BaseAddress?.ToString() ?? throw new InvalidOperationException("YARP base URL not configured");

    ConfigureServices(services, baseUrl);

    ServiceProvider = services.BuildServiceProvider();
    ScopedSender = new ScopedSender(ServiceProvider);
  }

  private static void ConfigureServices(IServiceCollection services, string baseUrl)
  {
    // Build configuration
    IConfiguration configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", optional: true)
      .Build();

    // Register FluentUI services (required by toast notifications)
    services.AddFluentUIComponents();

    // Add only the core services needed for testing (avoid service discovery conflicts)
    // Register TimeWarp State management (includes mediator services)
    services.AddTimeWarpState
    (
      options =>
      {
        options.Assemblies = new[]
        {
          typeof(Web.Spa.AssemblyMarker).Assembly
        };
      }
    );

    // Add HttpClient pointing to the YARP gateway from Aspire
    services.AddHttpClient(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName, c => c.BaseAddress = new Uri(baseUrl));

    // Configure JSON serializer options
    services.Configure<JsonSerializerOptions>(options =>
    {
      options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

    // Register IAccessTokenProvider (required for API service)
    IAccessTokenProvider fakeAccessTokenProvider = A.Fake<IAccessTokenProvider>();
    services.AddScoped(_ => fakeAccessTokenProvider);

    // Register IApiServerApiService (required for handlers that call the API)
    services.AddScoped<IApiServerApiService>(serviceProvider =>
    {
      IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
      IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
      IOptions<JsonSerializerOptions> jsonOptions = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

      return new ApiServerApiService(httpClientFactory, accessTokenProvider, jsonOptions);
    });

    // Replace JSRuntime with a fake for testing
    IJSRuntime fakeJsRuntime = A.Fake<IJSRuntime>();
    services.AddScoped(_ => fakeJsRuntime);
  }

  public Task<TResponse> Send<TResponse>
  (
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default
  ) => ScopedSender.Send(request, cancellationToken);

  public Task<object> Send(object request, CancellationToken cancellationToken = default) =>
    ScopedSender.Send(request, cancellationToken);
}
