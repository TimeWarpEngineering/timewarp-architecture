namespace TimeWarp.Architecture.Api.Server;

public class Program : IAspNetProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp.Architecture Api.Server API {SwaggerVersion}";
  const string SwaggerBasePath = "api/api-server";
  const string SwaggerEndpoint = $"/swagger/{SwaggerVersion}/swagger.json";

  public static Task<int> Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);
    builder.AddServiceDefaults();

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.MapDefaultEndpoints();

    webApplication.Services.ValidateOptions(builder.Services, webApplication.Logger);

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunOaktonCommands(aArgumentArray);
  }

  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager)
  {
    CommonServerModule.ConfigureConfiguration(aConfigurationManager);
  }

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    CommonServerModule.ConfigureServices(aServiceCollection, aConfiguration);
    ConfigureSettings(aServiceCollection, aConfiguration);

    CorsPolicy.Any.Apply(aServiceCollection);

    aServiceCollection
      .AddControllers()
      .TryAddApplicationPart(typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker).Assembly);

    aServiceCollection.AddFluentValidationAutoValidation();
    aServiceCollection.AddFluentValidationClientsideAdapters();

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    aServiceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Server.AssemblyMarker>();
    aServiceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Contracts.AssemblyMarker>();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    aServiceCollection.AddEndpointsApiExplorer();
    aServiceCollection.AddSwaggerGen();

    aServiceCollection
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
        aServiceCollection,
        SwaggerVersion,
        SwaggerApiTitle,
        [typeof(TimeWarp.Architecture.Api.Server.AssemblyMarker), typeof(TimeWarp.Architecture.Api.Contracts.AssemblyMarker)]
      );
  }

  public static void ConfigureMiddleware(WebApplication aWebApplication)
  {
    CommonServerModule.ConfigureMiddleware(aWebApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Sometimes, you might want to allow other sites to make cross-origin requests to your app.
    if (aWebApplication.Environment.IsDevelopment())
    {
      aWebApplication.UseCors(CorsPolicy.Any.Name);
    }

    CommonServerModule.UseSwaggerUi(aWebApplication, SwaggerBasePath, SwaggerEndpoint, SwaggerApiTitle);

    //aWebApplication.UseHttpsRedirection(); // In K8s we won't use https so we don't want to redirect

    aWebApplication.UseAuthorization();
  }

  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    CommonServerModule.ConfigureEndpoints(aWebApplication);

    aWebApplication.MapControllers();
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    //aServiceCollection
    //  .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration)
    //  .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);
  }
}
