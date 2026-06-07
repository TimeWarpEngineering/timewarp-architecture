namespace TimeWarp.Architecture;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    ConfigureAzureAppConfig(configurationManager); ;
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    IConfigurationRoot configurationRoot = webApplication!.Configuration as IConfigurationRoot ?? throw new InvalidOperationException();

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.MapGet
      (
        "/api/debug-config",
        httpContext =>
        {
          string? config = configurationRoot.GetDebugView();
          return httpContext.Response.WriteAsync(config);
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

  public static void AddOpenApi
  (
    IServiceCollection serviceCollection,
    string apiVersion,
    string apiTitle,
    Type[] typeArray
  )
  {
    // Scalar will generate OpenAPI from API controllers automatically
    serviceCollection.AddEndpointsApiExplorer();
  }

  public static void UseScalarApiReference
  (
    WebApplication webApplication,
    string apiVersion,
    string apiTitle
  )
  {
    webApplication.MapScalarApiReference();
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
            azureAppConfigurationKeyVaultOptions =>
              azureAppConfigurationKeyVaultOptions.SetCredential(new DefaultAzureCredential())
          ),
      optional: false
    );

    string? testValue = configurationManager.GetValue<string>("TestValue");
    Console.WriteLine($"App Config value TestValue: {testValue}");
  }

}
