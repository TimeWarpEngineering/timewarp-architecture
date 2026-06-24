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
