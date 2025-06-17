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
    WebTestServerApplication aWebTestServerApplication
#if(api)
    ,ApiTestServerApplication aApiTestServerApplication
#endif
  ) :
  base
  (
    new WebApplicationHost<Yarp.Server.Program>
    (
      aUrls: new[]
      {
        "https://localhost:8443"
      },
      aWebApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Yarp.Server.AssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
      ConfigureServicesCallback
    )
  )
  {
    WebTestServerApplication = aWebTestServerApplication;
#if(api)
    ApiTestServerApplication = aApiTestServerApplication;
#endif
  }

  protected static void ConfigureServicesCallback(IServiceCollection aServiceCollection) { }
}
