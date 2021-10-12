namespace TimeWarp.Blazor.Testing
{
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;
  using TimeWarp.Blazor.Server;

  public class TimeWarpBlazorServerApplication : TestApplication<Startup>
  {
    public TimeWarpBlazorServerApplication() :
    base
    (
      new Application<Startup>
      (
        aEnvironmentName: "Development",
        aUrls: new[]
        {
          "http://localhost:5000"
        },
        aApplicationName: typeof(Startup).Namespace,
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
}

