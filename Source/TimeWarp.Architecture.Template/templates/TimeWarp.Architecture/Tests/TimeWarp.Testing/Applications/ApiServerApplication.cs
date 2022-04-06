namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class ApiServerApplication : TestServerApplication<Api.Server.Program>
{
  public ApiServerApplication() :
  base
  (
    new WebApplicationHost<Api.Server.Program>
    (
      aEnvironmentName: Environments.Development,
      aUrls: new[]
      {
        "https://localhost:7255"
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
