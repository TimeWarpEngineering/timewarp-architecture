namespace TimeWarp.Architecture.Api.Server;

using Behaviors;
using MediatR;

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

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Server.IAssemblyMarker>();
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Contracts.IAssemblyMarker>();

    serviceCollection.AddFastEndpoints(options =>
    {
      options.IncludeAbstractValidators = false; //This will run all AbstractValidators in the FastEndpoints pipeline.
      options.Assemblies = new[]
      {
        typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Api.Contracts.IAssemblyMarker).Assembly
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
    serviceCollection
      .AddMediatR
      (
        mediatRServiceConfiguration =>
          mediatRServiceConfiguration
            .RegisterServicesFromAssemblies
            (
              typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).GetTypeInfo().Assembly,
              typeof(TimeWarp.Architecture.Api.Application.IAssemblyMarker).GetTypeInfo().Assembly
            )
      );
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(GenericPipelineBehavior<,>));
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    webApplication.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
    });
    webApplication.UseAuthorization();

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
      webApplication.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
      webApplication.MapScalarApiReference();
    }
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    CommonServerModule.ConfigureEndpoints(webApplication);
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    //serviceCollection
    //  .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(configuration)
    //  .ConfigureOptions<SampleOptions, SampleOptionsValidator>(configuration);
  }
}
