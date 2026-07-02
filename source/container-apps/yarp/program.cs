#region Purpose
// Entry point for the YARP reverse-proxy gateway that fronts the container apps.
#endregion

#region Design
// All routing lives in the "ReverseProxy" configuration section, not code — operators can
// reshape routes/clusters without a rebuild.
// AddServiceDiscoveryDestinationResolver lets cluster destinations use Aspire logical service
// names instead of hard-coded addresses.
// Implements IAspNetProgram so every container app shares the same Configure* phase structure,
// keeping the template's entry points uniform; unused phases stay as empty methods rather than
// being removed.
#endregion

namespace TimeWarp.Architecture.Yarp.Server;

public class Program : IAspNetProgram
{
  public static Task Main(string[] argumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);

    // This line should be sufficient for HTTPS configuration in Aspire with .NET 8
    builder.WebHost.UseKestrelHttpsConfiguration();

    builder.AddServiceDefaults();
    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.MapDefaultEndpoints();

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunAsync();
  }
  public static void ConfigureConfiguration(ConfigurationManager configurationManager) {}
  public static void ConfigureEndpoints(WebApplication webApplication) {}

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    webApplication.MapReverseProxy();
  }

  public static void ConfigureServices
  (
    IServiceCollection serviceCollection,
    IConfiguration configuration
  )
  {
    serviceCollection
      .AddReverseProxy()
      .LoadFromConfig(configuration.GetSection("ReverseProxy"))
      .AddServiceDiscoveryDestinationResolver();
  }
}
