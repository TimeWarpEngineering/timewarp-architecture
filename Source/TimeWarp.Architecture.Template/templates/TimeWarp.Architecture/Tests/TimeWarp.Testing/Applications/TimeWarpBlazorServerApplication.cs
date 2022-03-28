namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Server;

/// <summary>
/// Used to launch the TimeWarp.Architecture.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class TimeWarpBlazorServerApplication : TestServerApplication<Startup>
{
  public TimeWarpBlazorServerApplication() :
  base
  (
    new WebApplication<Startup>
    (
      aEnvironmentName: "Development",
      aUrls: new[]
      {
        "http://localhost:5000"
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
