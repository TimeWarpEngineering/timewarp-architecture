namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesCallback"/></remarks>
public class YarpTestServerApplication : TestServerApplication<Yarp.Server.Program>
{
#if(web)
  private readonly WebTestServerApplication WebTestServerApplication;
#endif
#if(api)
  private readonly ApiTestServerApplication ApiTestServerApplication;
#endif
  internal const int YarpPort = 8443;

  /// <param name="configureServices">
  /// Optional extras after the built-in test wiring (C-create / <see cref="HostGraphFactory"/>).
  /// </param>
  public YarpTestServerApplication
  (
#if(web)
    WebTestServerApplication webTestServerApplication
#endif
#if(web && api)
    ,
#endif
#if(api)
    ApiTestServerApplication apiTestServerApplication
#endif
#if(web || api)
    ,
#endif
    Action<IServiceCollection>? configureServices = null
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
          ContentRootPath = Path.GetDirectoryName(
            typeof(TimeWarp.Architecture.Yarp.Server.IAssemblyMarker).Assembly.Location),
        },
      services =>
      {
        ConfigureServicesCallback(services);
        configureServices?.Invoke(services);
      }
    )
  )
  {
#if(web)
    WebTestServerApplication = webTestServerApplication;
#endif
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

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Yarp.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}
