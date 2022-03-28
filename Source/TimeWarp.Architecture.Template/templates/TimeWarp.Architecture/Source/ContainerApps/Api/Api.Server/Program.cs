using FluentValidation.AspNetCore;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Oakton;
using System.Reflection;
using TimeWarp.Architecture.Features;

string swaggerVersion = "v1";
string swaggerApiTitle = $"TimeWarp Architecture API {swaggerVersion}";
string swaggerEndPoint = $"/swagger/{swaggerVersion}/swagger.json";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigureConfiguration(builder.Configuration);
ConfigureServices(builder.Services);

WebApplication webApplication = builder.Build();

ConfigureMiddleware(webApplication, webApplication.Services);
ConfigureEndpoints(webApplication, webApplication.Services);

webApplication.RunOaktonCommandsSynchronously(args);

void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { };

void ConfigureServices(IServiceCollection aServiceCollection)
{
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

void ConfigureMiddleware(IApplicationBuilder aApplicationBuilder, IServiceProvider aServiceCollection)
{
  webApplication.UseSwagger();
  webApplication.UseSwaggerUI
  (
    aSwaggerUIOptions => aSwaggerUIOptions.SwaggerEndpoint(swaggerEndPoint, swaggerApiTitle)
  );

  webApplication.UseHttpsRedirection();

  webApplication.UseAuthorization();
}

void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection)
{
  webApplication.MapControllers();
}

void ConfigureSwagger(IServiceCollection aServiceCollection)
{
  // Register the Swagger generator, defining 1 or more Swagger documents
  aServiceCollection.AddSwaggerGen
    (
      aSwaggerGenOptions =>
      {
        aSwaggerGenOptions
        .SwaggerDoc
        (
          swaggerVersion,
          new OpenApiInfo { Title = swaggerApiTitle, Version = swaggerVersion }
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
