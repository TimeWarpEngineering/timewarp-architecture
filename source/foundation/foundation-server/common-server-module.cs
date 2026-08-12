#region Purpose
// Server plumbing shared by every container app so hosts compose it instead of duplicating it.
#endregion

#region Design
// Implements IAspNetModule's static hooks so each server's Program composes modules uniformly.
// ConfigureConfiguration is a no-op host hook: configuration-source composition (Azure App
// Config, Key Vault, custom providers) belongs in the host's Program, not this published
// foundation package — libraries must not Console.Write credentials or force Azure package
// weight on every consumer (task 131 F-001). /api/debug-config is Development-only because
// GetDebugView exposes secrets. The FluentValidation DisplayNameResolver emits "Type:Member"
// so validation errors disambiguate identically named properties across contracts.
// ConfigureServices Applies ContractSerializationDefaults to HttpJsonOptions so the host
// wire shape matches the SPA/CLI/tests seam (camelCase properties; PascalCase string enums).
// Mvc.JsonOptions was removed with MVC BaseEndpoint (task 131 F-002) — no MVC consumers.
// OpenAPI: FastEndpoints.OpenApi's OpenApiDocument (not raw services.AddOpenApi) so FE
// transformers and endpoint metadata flow into the document. OpenAPI/Scalar feature tags
// come from generator Description.WithTags (namespace leaf under Features, plus additive
// [OpenApiTags]); FE Tags() is filter-only and does not set OpenAPI operation tags.
// AutoTagPathSegmentIndex=0 disables route-segment auto-tags so they do not compete with
// those explicit OpenAPI tags. UseScalarApiReference must run after UseFastEndpoints;
// MapOpenApi serves /openapi/{doc}.json and MapScalarApiReference points at that
// DocumentName via AddDocument.
#endregion

namespace TimeWarp.Foundation;

public class CommonServerModule : IAspNetModule
{
  /// <summary>
  /// Host hook for additional configuration sources. Intentionally empty in the foundation
  /// package — apps that need Azure App Config (or similar) register it in their own Program.
  /// </summary>
  public static void ConfigureConfiguration(ConfigurationManager configurationManager) { }

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
      {
        _ = lambdaExpression;
        return type != null && memberInfo != null ? $"{type.Name}:{memberInfo.Name}" : null;
      };

    // Contract-seam serialization (camelCase properties + PascalCase string enums). Without this,
    // the server would emit default STJ integers while the SPA/CLI/tests use ContractSerializationDefaults.
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
    // FastEndpoints.OpenApi — not services.AddOpenApi() — so FE transformers and Description.WithTags metadata wire up.
    serviceCollection.OpenApiDocument
    (
      options =>
      {
        options.DocumentName = apiVersion;
        options.Title = apiTitle;
        options.Version = apiVersion;
        // Generator emits OpenAPI tags via Description.WithTags (…Features.<Id> leaf / [OpenApiTags]).
        // Disable route-segment auto-tags so they do not compete with those explicit tags.
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
}
