namespace TimeWarp.Architecture.Api.Server;

using FluentValidation.AspNetCore;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Oakton;
using System.Reflection;
using TimeWarp.Architecture.CorsPolicies;

public class Program : IAspNetProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp.Architecture Api.Server API {SwaggerVersion}";
  const string SwaggerEndPoint = $"/swagger/{SwaggerVersion}/swagger.json";

  public static Task<int> Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.Services.ValidateOptions(builder.Services);

    ConfigureMiddleware(webApplication, webApplication.Services, webApplication.Environment);
    ConfigureEndpoints(webApplication, webApplication.Services);

    return webApplication.RunOaktonCommands(aArgumentArray);
  }

  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager)
  {
    CommonServerModule.ConfigureConfiguration(aConfigurationManager);
    ;
  }

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    CommonServerModule.ConfigureServices(aServiceCollection, aConfiguration);
    ConfigureSettings(aServiceCollection, aConfiguration);

    CorsPolicy.Any.Apply(aServiceCollection);

    aServiceCollection
      .AddControllers()
      .TryAddApplicationPart(typeof(Api_Server_Assembly).Assembly)
      .AddFluentValidation
        (
          aFluentValidationMvcConfiguration =>
          {
            // RegisterValidatorsFromAssemblyContaining will register all public Validators as scoped but
            // will NOT register internals. This feature is utilized.
            aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Api_Server_Assembly>();
            aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Api_Contracts_Assembly>();
          }
        );

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    aServiceCollection.AddEndpointsApiExplorer();
    aServiceCollection.AddSwaggerGen();

    aServiceCollection
      .AddMediatR
      (
        typeof(Api_Server_Assembly).GetTypeInfo().Assembly,
        typeof(Api_Application_Assembly).GetTypeInfo().Assembly
      );

    ConfigureSwagger(aServiceCollection);
  }

  public static void ConfigureMiddleware(WebApplication aWebApplication, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment)
  {
    CommonServerModule.ConfigureMiddleware(aWebApplication, aServiceCollection, aHostEnvironment);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Sometimes, you might want to allow other sites to make cross-origin requests to your app.
    if (aHostEnvironment.IsDevelopment())
    {
      aWebApplication.UseCors(CorsPolicy.Any.Name);
    }

    aWebApplication.UseSwagger();
    aWebApplication.UseSwaggerUI
    (
      aSwaggerUIOptions => aSwaggerUIOptions.SwaggerEndpoint(SwaggerEndPoint, SwaggerApiTitle)
    );

    aWebApplication.UseHttpsRedirection();

    aWebApplication.UseAuthorization();
  }

  public static void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection)
  {
    CommonServerModule.ConfigureEndpoints(aEndpointRouteBuilder, aServiceCollection);
    aEndpointRouteBuilder.MapControllers();
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    //aServiceCollection
    //  .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration)
    //  .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);
  }
  private static void ConfigureSwagger(IServiceCollection aServiceCollection)
  {
    // Register the Swagger generator, defining 1 or more Swagger documents
    aServiceCollection.AddSwaggerGen
      (
        aSwaggerGenOptions =>
        {
          aSwaggerGenOptions
          .SwaggerDoc
          (
            SwaggerVersion,
            new OpenApiInfo { Title = SwaggerApiTitle, Version = SwaggerVersion }
          );

          aSwaggerGenOptions.EnableAnnotations();

          string xmlFile = $"{typeof(Api_Server_Assembly).Assembly.GetName().Name}.xml";
          string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);

          xmlFile = $"{typeof(Api_Contracts_Assembly).Assembly.GetName().Name}.xml";
          xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);
        }
      );

    aServiceCollection.AddFluentValidationRulesToSwagger();
  }
}
