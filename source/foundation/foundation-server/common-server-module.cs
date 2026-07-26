#region Purpose
// Server plumbing shared by every container app so hosts compose it instead of duplicating it.
#endregion

#region Design
// Implements IAspNetModule's static hooks so each server's Program composes modules uniformly.
// Azure App Config is opt-in: absent "AppConfig" connection string means local config only, so
// the template runs without any Azure dependency; refresh keys off a "Sentinel" value to avoid
// per-key registration. /api/debug-config is Development-only because GetDebugView exposes
// secrets. The FluentValidation DisplayNameResolver emits "Type:Member" so validation errors
// disambiguate identically named properties across contracts. ConfigureServices also Applies
// ContractSerializationDefaults to MVC JsonOptions and HttpJsonOptions so the host wire shape
// matches the SPA/CLI/tests seam (camelCase properties; PascalCase string enums).
// OpenAPI: FastEndpoints.OpenApi's OpenApiDocument (not raw services.AddOpenApi) so FE
// transformers and endpoint metadata (including generator-emitted Tags from Features.* leaf
// namespaces) flow into the document. AutoTagPathSegmentIndex=0 disables route-segment
// auto-tags so only explicit Tags() drive Scalar's feature-grouped sidebar.
// UseScalarApiReference must run after UseFastEndpoints; MapOpenApi serves /openapi/{doc}.json
// and MapScalarApiReference points at that DocumentName via AddDocument.
#endregion

namespace TimeWarp.Foundation;

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

    // Contract-seam serialization (camelCase properties + PascalCase string enums). Without this,
    // the server would emit default STJ integers while the SPA/CLI/tests use ContractSerializationDefaults.
    serviceCollection.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(
      o => ContractSerializationDefaults.Apply(o.JsonSerializerOptions));
    serviceCollection.ConfigureHttpJsonOptions(
      o => ContractSerializationDefaults.Apply(o.SerializerOptions));
  }

  /// <summary>
  /// Registers a FastEndpoints OpenAPI document for Scalar (and any other document consumers).
  /// </summary>
  /// <param name="serviceCollection">Host service collection.</param>
  /// <param name="apiVersion">Document name and version (e.g. "v1"); must match AddDocument.</param>
  /// <param name="apiTitle">Human-readable document title shown in Scalar.</param>
  public static void AddOpenApi
  (
    IServiceCollection serviceCollection,
    string apiVersion,
    string apiTitle
  )
  {
    // FastEndpoints.OpenApi — not services.AddOpenApi() — so FE transformers and Tags() metadata wire up.
    serviceCollection.OpenApiDocument
    (
      options =>
      {
        options.DocumentName = apiVersion;
        options.Title = apiTitle;
        options.Version = apiVersion;
        // Generator already emits feature Tags from …Features.<Id>; disable route-segment auto-tags.
        options.AutoTagPathSegmentIndex = 0;
        options.ExcludeNonFastEndpoints = true;
      }
    );
  }

  /// <summary>
  /// Maps the OpenAPI document endpoint and Scalar UI. Call after <c>UseFastEndpoints</c>.
  /// </summary>
  /// <param name="webApplication">Built host.</param>
  /// <param name="apiVersion">Document name registered in <see cref="AddOpenApi"/>.</param>
  /// <param name="apiTitle">UI title and document display name.</param>
  public static void UseScalarApiReference
  (
    WebApplication webApplication,
    string apiVersion,
    string apiTitle
  )
  {
    webApplication.MapOpenApi();
    webApplication.MapScalarApiReference
    (
      options =>
      {
        options
          .WithTitle(apiTitle)
          .AddDocument(apiVersion, apiTitle)
          .SortTagsAlphabetically()
          .ExpandAllTags();
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
            azureAppConfigurationKeyVaultOptions =>
              azureAppConfigurationKeyVaultOptions.SetCredential(new DefaultAzureCredential())
          ),
      optional: false
    );

    string? testValue = configurationManager.GetValue<string>("TestValue");
    Console.WriteLine($"App Config value TestValue: {testValue}");
  }

}
