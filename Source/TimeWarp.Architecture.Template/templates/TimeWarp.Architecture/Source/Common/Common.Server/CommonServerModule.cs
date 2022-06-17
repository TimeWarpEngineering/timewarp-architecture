namespace TimeWarp.Architecture;

using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }
  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    var configurationRoot = aWebApplication!.Configuration as IConfigurationRoot;

    if (aWebApplication.Environment.IsDevelopment())
    {
      aWebApplication.MapGet
      (
        "/api/debug-config",
        aHttpContext =>
        {
          string? config = configurationRoot.GetDebugView();
          return aHttpContext.Response.WriteAsync(config);
        }
      );
    }
  }
  public static void ConfigureMiddleware(WebApplication aWebApplication) { }
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ValidatorOptions.Global.DisplayNameResolver =
      (aType, aMemberInfo, aLambdaExpression) =>
        aType != null && aMemberInfo != null ? $"{aType.Name}:{aMemberInfo.Name}" : null;
  }

  public static void AddSwaggerGen
  (
    IServiceCollection aServiceCollection,
    string aSwaggerVersion,
    string aSwaggerApiTitle,
    Type[] aTypeArray
  )
  {
    aServiceCollection.AddSwaggerGen
      (
        aSwaggerGenOptions =>
        {
          aSwaggerGenOptions
          .SwaggerDoc
          (
            aSwaggerVersion,
            new OpenApiInfo { Title = aSwaggerApiTitle, Version = aSwaggerVersion }
          );

          aSwaggerGenOptions.EnableAnnotations();

          foreach(Type? assemblyType in aTypeArray)
          {
            string xmlFile = $"{assemblyType.Assembly.GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            aSwaggerGenOptions.IncludeXmlComments(xmlPath);
          }
        }
      );

    aServiceCollection.AddFluentValidationRulesToSwagger();
  }

  public static void UseSwaggerUi
  (
    WebApplication aWebApplication,
    string aSwaggerBasePath,
    string aSwaggerEndPoint,
    string aSwaggerApiTitle
  )
  {
    aWebApplication
      .UseSwagger
      (
        aSwaggerOptions => aSwaggerOptions.RouteTemplate = aSwaggerBasePath + "/swagger/{documentName}/swagger.json"
      )
      .UseSwaggerUI
      (
        aSwaggerUIOptions =>
        {
          aSwaggerUIOptions.SwaggerEndpoint($"/{aSwaggerBasePath}{aSwaggerEndPoint}", aSwaggerApiTitle);
          aSwaggerUIOptions.RoutePrefix = $"{aSwaggerBasePath}/swagger";
        }
      );
  }
}
