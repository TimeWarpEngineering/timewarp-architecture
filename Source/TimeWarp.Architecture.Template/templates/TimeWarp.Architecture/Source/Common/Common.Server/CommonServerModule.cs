namespace TimeWarp.Architecture;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager)
  {
    ConfgureAzureAppConfig(aConfigurationManager);;
  }

  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    IConfigurationRoot configurationRoot = (aWebApplication!.Configuration as IConfigurationRoot)!;

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

          foreach (Type? assemblyType in aTypeArray)
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
    string aSwaggerEndpoint,
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
          aSwaggerUIOptions.SwaggerEndpoint($"/{aSwaggerBasePath}{aSwaggerEndpoint}", aSwaggerApiTitle);
          aSwaggerUIOptions.RoutePrefix = $"{aSwaggerBasePath}/swagger";
        }
      );
  }

  private static void ConfgureAzureAppConfig(ConfigurationManager aConfigurationManager)
  {
    string? connectionString = aConfigurationManager.GetConnectionString("AppConfig");
    if (string.IsNullOrEmpty(connectionString))
    {
      Console.WriteLine("No AppConfig ConnectionString");
      return;
    }

    Console.WriteLine($"connectionString: {connectionString}");

    aConfigurationManager.AddAzureAppConfiguration
    (
      aAzureAppConfigurationOptions =>
        aAzureAppConfigurationOptions
          .Connect(connectionString)
          .UseFeatureFlags()
          .ConfigureRefresh
          (
            aAzureAppConfigurationRefreshOptions =>
              aAzureAppConfigurationRefreshOptions
                .Register("Sentinel", refreshAll: true)
                .SetCacheExpiration(TimeSpan.FromMinutes(5))
          )
          .ConfigureKeyVault
          (
            aAzureAppConfigurationKeyVaultOptions =>
              aAzureAppConfigurationKeyVaultOptions.SetCredential(new DefaultAzureCredential())
          ),
      optional: false
    );

    string? testValue = aConfigurationManager.GetValue<string>("TestValue");
    Console.WriteLine($"App Config value TestValue: {testValue}");
  }

}
