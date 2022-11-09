namespace TimeWarp.Architecture.Testing;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesCallback"/></remarks>
public class YarpTestServerApplication : TestServerApplication<Yarp.Server.Program>
{
  private readonly WebTestServerApplication WebTestServerApplication;
  private readonly ApiTestServerApplication ApiTestServerApplication;
  public YarpTestServerApplication
  (
    WebTestServerApplication aWebTestServerApplication,
    ApiTestServerApplication aApiTestServerApplication
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
          ApplicationName = typeof(Yarp_Server_Assembly).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
      ConfigureServicesCallback
    )
  )
  {
    WebTestServerApplication = aWebTestServerApplication;
    ApiTestServerApplication = aApiTestServerApplication;
  }

  protected static void ConfigureServicesCallback(IServiceCollection aServiceCollection) { }
}
