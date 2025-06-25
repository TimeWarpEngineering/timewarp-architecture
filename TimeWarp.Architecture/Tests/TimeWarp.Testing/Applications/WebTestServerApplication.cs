namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Web.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class WebTestServerApplication : TestServerApplication<Web.Server.Program>
{
  public WebTestServerApplication() :
  base
  (
    new WebApplicationHost<Web.Server.Program>
    (
      aUrls: new[]
      {
        "https://localhost:7000"
      },
      aWebApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
        },
      ConfigureServicesCallback
    )
  )
  { }

  protected static void ConfigureServicesCallback(IServiceCollection aServiceCollection) { }
}
