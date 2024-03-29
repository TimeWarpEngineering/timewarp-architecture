namespace TimeWarp.Architecture.Yarp.Server;

public class Program : IAspNetProgram
{
  public static Task Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);
    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunAsync();
  }
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }
  public static void ConfigureEndpoints(WebApplication aWebApplication) { }

  public static void ConfigureMiddleware(WebApplication aWebApplication) => aWebApplication.MapReverseProxy();

  public static void ConfigureServices
  (
    IServiceCollection aServiceCollection,
    IConfiguration aConfiguration
  )
  {
    aServiceCollection
      .AddReverseProxy()
      .LoadFromConfig(aConfiguration.GetSection("ReverseProxy"));
  }
}
