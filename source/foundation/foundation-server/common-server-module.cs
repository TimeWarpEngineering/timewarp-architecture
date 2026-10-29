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
// those explicit OpenAPI tags. EnableJWTBearerAuth stays off: the automatic scheme is
// named JWTBearerAuth and FastEndpoints attaches it only when AuthSchemeNames contains
// that string, which product endpoints never do (they list agent-token). AddAuth
// registers JWTBearerAuth when the host passes OpenApiAgentBearer, and
// OpenApiBearerSecurity copies it onto operations that list that scheme, with the
// host's policy-to-scope map. UseScalarApiReference must run after UseFastEndpoints;
// MapOpenApi serves /openapi/{doc}.json and MapScalarApiReference points at that
// DocumentName via AddDocument, with JWTBearerAuth as the preferred scheme so Authorize
// applies the bearer. OpenApiPublicServer then replaces servers with the
// forwarded public origin. Scalar's Servers list stays empty and follows that document.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Shared ASP.NET server module: contract JSON defaults, optional debug config, OpenAPI/Scalar helpers.
/// </summary>
public class CommonServerModule : IAspNetModule
{
  /// <summary>
  /// Host hook for additional configuration sources. Intentionally empty in the foundation
  /// package — apps that need Azure App Config (or similar) register it in their own Program.
  /// </summary>
  public static void ConfigureConfiguration(ConfigurationManager configurationManager) { }

  /// <summary>
  /// Maps Development-only <c>/api/debug-config</c> that dumps configuration (may include secrets).
  /// </summary>
  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    IConfigurationRoot configurationRoot = webApplication.Configuration as IConfigurationRoot ?? throw new InvalidOperationException();

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
  /// <summary>
  /// No shared middleware; hosts add their own pipeline.
  /// </summary>
  public static void ConfigureMiddleware(WebApplication webApplication) { }
  /// <summary>
  /// Applies FluentValidation display names and <see cref="ContractSerializationDefaults"/> to HTTP JSON.
  /// </summary>
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
    AddOpenApi(serviceCollection, apiVersion, apiTitle, null);
  }

  /// <summary>
  /// Registers a FastEndpoints OpenAPI document for Scalar (and any other document consumers).
  /// </summary>
  /// <param name="serviceCollection">Host service collection.</param>
  /// <param name="apiVersion">Document name and version (e.g. "v1"); must match AddDocument.</param>
  /// <param name="apiTitle">Human-readable document title shown in Scalar.</param>
  /// <param name="agentBearer">
  /// Scheme name and policy-to-scope map. When null, the document does not advertise agent bearer auth.
  /// </param>
  public static void AddOpenApi
  (
    IServiceCollection serviceCollection,
    string apiVersion,
    string apiTitle,
    OpenApiAgentBearer? agentBearer
  )
  {
    if (agentBearer is not null)
    {
      serviceCollection.AddSingleton(agentBearer);
    }

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
        // The automatic JWTBearerAuth scheme is never referenced: FastEndpoints matches it
        // against AuthSchemeNames, and those names are the host's agent scheme.
        options.EnableJWTBearerAuth = false;
        if (agentBearer is not null)
        {
          options.AddAuth
          (
            OpenApiBearerSecurity.SchemeName,
            OpenApiBearerSecurity.CreateScheme(agentBearer.PolicyScopes.Values.ToList())
          );
        }

        options.ConfigureOpenApi = openApiOptions =>
        {
          if (agentBearer is not null)
          {
            OpenApiBearerSecurity.Register(openApiOptions, agentBearer);
          }

          OpenApiPublicServer.Register(openApiOptions);
        };
      }
    );
    // The server transformer reads the request through IHttpContextAccessor. Hosts that already
    // register it are unchanged (TryAdd).
    serviceCollection.AddHttpContextAccessor();
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
        if (webApplication.Services.GetService<OpenApiAgentBearer>() is not null)
        {
          options.AddPreferredSecuritySchemes([OpenApiBearerSecurity.SchemeName]);
        }
      }
    );
  }
}
