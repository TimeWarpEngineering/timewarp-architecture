namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class YarpServerApplication : TestServerApplication<Yarp.Server.Program>
{
  private readonly WebServerApplication WebServerApplication;
  private readonly ApiServerApplication ApiServerApplication;
  public YarpServerApplication
  (
    WebServerApplication aWebServerApplication,
    ApiServerApplication aApiServerApplication
  ) :
  base
  (
    new WebApplicationHost<Yarp.Server.Program>
    (
      aEnvironmentName: Environments.Development,
      aUrls: new[]
      {
        "https://localhost:8443"
      },
      ConfigureServicesDelegate
    )
  )
  {
    WebServerApplication = aWebServerApplication;
    ApiServerApplication = aApiServerApplication;
  }

  protected static void ConfigureServicesDelegate
  (
    HostBuilderContext aHostBuilderContext,
    IServiceCollection aServiceCollection
  )
  { }
}
