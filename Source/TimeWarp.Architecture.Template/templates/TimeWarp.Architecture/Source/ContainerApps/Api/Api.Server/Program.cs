namespace TimeWarp.Architecture.Api;

using FluentValidation.AspNetCore;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Oakton;
using System.Reflection;
using TimeWarp.Architecture.Features;

public class Program : IProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp Architecture API {SwaggerVersion}";
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

  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ConfigureSettings(aServiceCollection, aConfiguration);

    aServiceCollection
      .AddControllers()
      .AddFluentValidation
        (
          aFluentValidationMvcConfiguration =>
          {
            // RegisterValidatorsFromAssemblyContaining will register all public Validators as scoped but
            // will NOT register internals. This feature is utilized.
            aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Program>();
            aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<BaseRequest>();
          }
        );

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    aServiceCollection.AddEndpointsApiExplorer();
    aServiceCollection.AddSwaggerGen();
    aServiceCollection.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
    ConfigureSwagger(aServiceCollection);
  }

  public static void ConfigureMiddleware(IApplicationBuilder aApplicationBuilder, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment)
  {
    aApplicationBuilder.UseSwagger();
    aApplicationBuilder.UseSwaggerUI
    (
      aSwaggerUIOptions => aSwaggerUIOptions.SwaggerEndpoint(SwaggerEndPoint, SwaggerApiTitle)
    );

    aApplicationBuilder.UseHttpsRedirection();

    aApplicationBuilder.UseAuthorization();
  }

  public static void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection) =>
    aEndpointRouteBuilder.MapControllers();

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    //aServiceCollection
    //  .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration)
    //  .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);
  }
  static void ConfigureSwagger(IServiceCollection aServiceCollection)
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

          // Set the comments path for the Swagger JSON and UI from Server.
          string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
          string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);

          // Set the comments path for the Swagger JSON and UI from Shared.
          xmlFile = $"{typeof(BaseRequest).Assembly.GetName().Name}.xml";
          xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);
        }
      );

    aServiceCollection.AddFluentValidationRulesToSwagger();
  }
}
