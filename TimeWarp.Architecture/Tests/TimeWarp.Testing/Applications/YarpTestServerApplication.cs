namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesCallback"/></remarks>
public class YarpTestServerApplication : TestServerApplication<Yarp.Server.Program>
{
  private readonly WebTestServerApplication WebTestServerApplication;
#if(api)
  private readonly ApiTestServerApplication ApiTestServerApplication;
#endif
  public YarpTestServerApplication
  (
    WebTestServerApplication webTestServerApplication
#if(api)
    ,ApiTestServerApplication apiTestServerApplication
#endif
  ) :
  base
  (
    new WebApplicationHost<Yarp.Server.Program>
    (
      urls:
      [
        "https://localhost:8443"
      ],
      webApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Yarp.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
      ConfigureServicesCallback
    )
  )
  {
    WebTestServerApplication = webTestServerApplication;
#if(api)
    ApiTestServerApplication = apiTestServerApplication;
#endif
  }

  protected static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    // Add configuration-based endpoint provider for test environment URLs
    // This allows us to map service names to literal URLs in appsettings.json
    serviceCollection.AddConfigurationServiceEndpointProvider();
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Yarp.Server.Program> webApplicationHost)
  {
    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var apiService = new ApiServerApiService(HttpClient, new MockAccessTokenProvider(), jsonSerializerOptions);
    return new WebApiTestService(apiService);
  }
}
