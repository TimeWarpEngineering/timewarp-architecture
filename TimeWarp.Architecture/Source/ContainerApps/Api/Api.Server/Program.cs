namespace TimeWarp.Architecture.Api.Server;

public class Program : IAspNetProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp.Architecture Api.Server API {SwaggerVersion}";
  const string SwaggerBasePath = "api/api-server";
  const string SwaggerEndpoint = $"/swagger/{SwaggerVersion}/swagger.json";

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
    });
    serviceCollection.AddAuthorization();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    serviceCollection.AddEndpointsApiExplorer();
    serviceCollection.AddSwaggerGen();

    serviceCollection
      .AddMediatR
      (
        mediatRServiceConfiguration =>
          mediatRServiceConfiguration.RegisterServicesFromAssemblies
          (
            typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.Architecture.Api.Application.AssemblyMarker).GetTypeInfo().Assembly
          )
      );

    CommonServerModule
      .AddSwaggerGen
      (
        serviceCollection,
        SwaggerVersion,
        SwaggerApiTitle,
        [typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker), typeof(TimeWarp.Architecture.Api.Contracts.AssemblyMarker)]
      );
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Sometimes, you might want to allow other sites to make cross-origin requests to your app.
    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
    }

    CommonServerModule.UseSwaggerUi(webApplication, SwaggerBasePath, SwaggerEndpoint, SwaggerApiTitle);

    //aWebApplication.UseHttpsRedirection(); // In K8s we won't use https so we don't want to redirect

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
