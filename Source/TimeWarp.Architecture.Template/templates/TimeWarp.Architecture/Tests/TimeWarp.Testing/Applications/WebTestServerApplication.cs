namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
      aEnvironmentName: Environments.Development,
      aUrls: new[]
      {
        "https://localhost:7001"
      },
      ConfigureServicesDelegate
    )
  )
  { }

  protected static void ConfigureServicesDelegate
  (
    HostBuilderContext aHostBuilderContext,
    IServiceCollection aServiceCollection
  )
  { }
}
