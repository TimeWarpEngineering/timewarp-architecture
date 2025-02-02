namespace TimeWarp.Architecture.Api.Server;

public class Program : IAspNetProgram
{
  const string ApiTitle = "TimeWarp.Architecture Api.Server API";
  const string ApiVersion = "v1";

  public static Task<int> Main(string[] argumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);
    builder.AddServiceDefaults();

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.MapDefaultEndpoints();

    webApplication.Services.ValidateOptions(builder.Services, webApplication.Logger);

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunOaktonCommands(argumentArray);
  }

  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    CommonServerModule.ConfigureConfiguration(configurationManager);
  }

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    CommonServerModule.ConfigureServices(serviceCollection, configuration);
    ConfigureSettings(serviceCollection, configuration);

    CorsPolicy.Any.Apply(serviceCollection);

    serviceCollection.AddFastEndpoints(options =>
    {
      options.Assemblies = new[]
      {
        typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Api.Contracts.AssemblyMarker).Assembly
      };
    })
    .SwaggerDocument(options =>
    {
      options.DocumentSettings = settings =>
      {
        settings.Title = ApiTitle;
        settings.Version = ApiVersion;
      };
      options.SerializerSettings = serializerSettings =>
      {
        serializerSettings.PropertyNamingPolicy = null;
      };
    });

    serviceCollection.AddAuthorization();
    serviceCollection.AddEndpointsApiExplorer();
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
      webApplication.UseOpenApi(c => c.Path = "/openapi/v1.json");
      webApplication.MapScalarApiReference();
    }

    webApplication.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
    });

    webApplication.UseAuthorization();
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    CommonServerModule.ConfigureEndpoints(webApplication);
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration aConfiguration)
  {
    //aServiceCollection
    //  .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration)
    //  .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);
  }
}
