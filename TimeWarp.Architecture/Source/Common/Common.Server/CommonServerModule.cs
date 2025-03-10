﻿namespace TimeWarp.Architecture;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    ConfigureAzureAppConfig(configurationManager);;
  }
  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    IConfigurationRoot configurationRoot = webApplication!.Configuration as IConfigurationRoot ?? throw new InvalidOperationException();

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.MapGet
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
  public static void ConfigureMiddleware(WebApplication webApplication) { }
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    ValidatorOptions.Global.DisplayNameResolver =
      (type, memberInfo, lambdaExpression) =>
        type != null && memberInfo != null ? $"{type.Name}:{memberInfo.Name}" : null;
  }

  public static void AddSwaggerGen
  (
    IServiceCollection serviceCollection,
    string swaggerVersion,
    string swaggerApiTitle,
    Type[] typeArray
  )
  {
    serviceCollection.AddSwaggerGen
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

          foreach (Type? assemblyType in typeArray)
          {
            string xmlFile = $"{assemblyType.Assembly.GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            aSwaggerGenOptions.IncludeXmlComments(xmlPath);
          }
        }
      );

    serviceCollection.AddFluentValidationRulesToSwagger();
  }

  public static void UseSwaggerUi
  (
    WebApplication webApplication,
    string swaggerBasePath,
    string swaggerEndpoint,
    string swaggerApiTitle
  )
  {
    webApplication
      .UseSwagger
      (
        aSwaggerOptions => aSwaggerOptions.RouteTemplate = swaggerBasePath + "/swagger/{documentName}/swagger.json"
      )
      .UseSwaggerUI
      (
        aSwaggerUIOptions =>
        {
          aSwaggerUIOptions.SwaggerEndpoint($"/{swaggerBasePath}{swaggerEndpoint}", swaggerApiTitle);
          aSwaggerUIOptions.RoutePrefix = $"{swaggerBasePath}/swagger";
        }
      );
  }

  private static void ConfigureAzureAppConfig(IConfigurationManager configurationManager)
  {
    string? connectionString = configurationManager.GetConnectionString("AppConfig");
    if (string.IsNullOrEmpty(connectionString))
    {
      Console.WriteLine("No AppConfig ConnectionString");
      return;
    }

    Console.WriteLine($"connectionString: {connectionString}");

    configurationManager.AddAzureAppConfiguration
    (
      azureAppConfigurationOptions =>
        azureAppConfigurationOptions
          .Connect(connectionString)
          .UseFeatureFlags()
          .ConfigureRefresh
          (
            azureAppConfigurationRefreshOptions =>
              azureAppConfigurationRefreshOptions
                .Register("Sentinel", refreshAll: true)
                .SetRefreshInterval(TimeSpan.FromMinutes(5))
          )
          .ConfigureKeyVault
          (
            aAzureAppConfigurationKeyVaultOptions =>
              aAzureAppConfigurationKeyVaultOptions.SetCredential(new DefaultAzureCredential())
          ),
      optional: false
    );

    string? testValue = configurationManager.GetValue<string>("TestValue");
    Console.WriteLine($"App Config value TestValue: {testValue}");
  }

}
