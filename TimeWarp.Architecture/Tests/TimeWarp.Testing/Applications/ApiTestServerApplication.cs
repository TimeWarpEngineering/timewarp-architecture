namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public sealed class ApiTestServerApplication : TestServerApplication<Api.Server.Program>
{
  public ApiTestServerApplication() :
    base
    (
      new WebApplicationHost<Api.Server.Program>
      (
        aUrls:
        [
          "https://localhost:7255"
        ],
        aWebApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
        ConfigureServicesCallback
      )
    )
  { }

  private static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHttpClient(); // This will give us the IHttpClientFactory
    serviceCollection.AddSingleton<IAccessTokenProvider, MockAccessTokenProvider>(); // This will give us the IAccessTokenProvider
  }
}
