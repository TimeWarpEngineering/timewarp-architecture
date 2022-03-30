namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Web.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class ApiServerApplication : TestServerApplication<Api.Program>
{
  public ApiServerApplication() :
  base
  (
    new WebApplicationHost<Api.Program>
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
