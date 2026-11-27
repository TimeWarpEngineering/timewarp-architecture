#region Purpose
// Server plumbing shared by every container app so hosts compose it instead of duplicating it.
#endregion

#region Design
// Implements IAspNetModule's static hooks so each server's Program composes modules uniformly.
// ConfigureConfiguration is a no-op host hook: configuration-source composition (Azure App
// Config, Key Vault, custom providers) belongs in the host's Program, not this published
// foundation package — libraries must not Console.Write credentials or force Azure package
// weight on every consumer (task 131 F-001). ConfigureEndpoints is a no-op host hook too: the
// module maps no endpoints, in any environment, because a configuration dump over HTTP would
// expose every environment variable and secret the process holds (task 524). The
// FluentValidation DisplayNameResolver emits "Type:Member" so validation errors disambiguate
// identically named properties across contracts.
// ConfigureServices Applies ContractSerializationDefaults to HttpJsonOptions so the host
// wire shape matches the SPA/CLI/tests seam (camelCase properties; PascalCase string enums).
// Mvc.JsonOptions was removed with MVC BaseEndpoint (task 131 F-002) — no MVC consumers.
// OpenAPI: FastEndpoints.OpenApi's OpenApiDocument (not raw services.AddOpenApi) so FE
// transformers and endpoint metadata flow into the document. OpenAPI/Scalar feature tags
// come from generator Description.WithTags (namespace leaf under Features, plus additive
// [OpenApiTags]); FE Tags() is filter-only and does not set OpenAPI operation tags.
// AutoTagPathSegmentIndex=0 disables route-segment auto-tags so they do not compete with
// those explicit OpenAPI tags. EnableJWTBearerAuth stays off and FastEndpoints AddAuth is not
// called: FastEndpoints' document pass writes every registered scheme onto an endpoint with no
// AuthSchemes, after the operation transformers ran. OpenApiOperationSecurity adds
// JWTBearerAuth when the host passes OpenApiAgentBearer and IdentitySessionCookie (apiKey in a
// cookie) when it passes OpenApiSessionCookie, and writes every operation's security from its
// AuthSchemeNames: bearer with the host's scopes, the session, both as alternatives, or [] for
// an anonymous verb. UseScalarApiReference must run after UseFastEndpoints;
// MapOpenApi serves /openapi/{doc}.json and MapScalarApiReference points at that
// DocumentName via AddDocument, with JWTBearerAuth as the preferred scheme so Authorize
// applies the bearer. OpenApiOneOf publishes the OneOf response union as its wire shape
// (success body at 200, SharedProblemDetails as problem+json). OpenApiProblemDetails completes
// that one problem component (errors, extension members), documents 400 on operations with a
// validator, and after the host callback checks that every problem+json response references it.
// OpenApiTypedId publishes every [TypedId] struct as string uuid, the Guid string its converter
// writes, and Nullable<T> of one as [null, string] uuid. OpenApiValidatorRequired publishes
// required from each request's validators (unconditional NotEmpty/NotNull) on the body schema
// and query parameters, and its document check, registered last, fails a request member that
// lost required.
// OpenApiPublicServer replaces
// the document's servers with the forwarded public origin, writes every server URL without a
// trailing slash, and Scalar's Servers list stays empty and follows that document.
// documentInfo (host-supplied) sets info.contact and info.license from the host assembly's
// repository metadata and names the root tag descriptions. OpenApiRootTags runs after the host
// callback and declares every operation tag at the root; its check, registered last, fails an
// undeclared tag, and an undescribed one when the host passed descriptions.
// documentPublicWebRoutes (web-server only) adds GET /api/health and the /api tip alias,
// and records /.well-known discovery documents and liveness in info.description plus
// x-timewarp-excluded-routes; excludedWebRoutes appends the host's own non-operation routes.
// ExcludeNonFastEndpoints stays on. Api-server does not opt in.
// OpenApiOperationNames writes each operationId and summary from the contract's [Operation]
// (the generated OperationName and OperationDescription constants), fills parameter and
// interface-declared property descriptions from XML <summary>, and its check, registered last,
// fails a repeated operationId and a backtick-quoted operation id in a description that no
// operation has.
// configureOpenApi runs after those transformers so a host can add product responses
// (payment 402/503) without putting product routes in this assembly.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Shared ASP.NET server module: contract JSON defaults and OpenAPI/Scalar helpers.
/// </summary>
public class CommonServerModule : IAspNetModule
{
  /// <summary>
  /// Host hook for additional configuration sources. Intentionally empty in the foundation
  /// package — apps that need Azure App Config (or similar) register it in their own Program.
  /// </summary>
  public static void ConfigureConfiguration(ConfigurationManager configurationManager) { }

  /// <summary>
  /// No shared endpoints; hosts map their own routes.
  /// </summary>
  public static void ConfigureEndpoints(WebApplication webApplication) { }

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
  /// <param name="documentPublicWebRoutes">
  /// When true, the document lists <c>GET /api/health</c> and the <c>GET|POST /api</c> tip alias,
  /// and records the <c>/.well-known/*</c> discovery documents and liveness as excluded routes.
  /// Api-server leaves this false: those routes are not on that host.
  /// </param>
  /// <param name="configureOpenApi">
  /// Host callback invoked after the shared transformers. Null adds none.
  /// </param>
  /// <param name="sessionCookie">
  /// Browser session scheme and cookie name. When null, the document does not advertise the session cookie.
  /// </param>
  /// <param name="excludedWebRoutes">
  /// Host routes that are served but are not operations. Listed only when
  /// <paramref name="documentPublicWebRoutes"/> is true.
  /// </param>
  /// <param name="documentInfo">
  /// Contact, license, and root tag descriptions. When null, root tags are declared without
  /// descriptions and info has no contact or license.
  /// </param>
  public static void AddOpenApi
  (
    IServiceCollection serviceCollection,
    string apiVersion,
    string apiTitle,
    OpenApiAgentBearer? agentBearer,
    bool documentPublicWebRoutes = false,
    Action<OpenApiOptions>? configureOpenApi = null,
    OpenApiSessionCookie? sessionCookie = null,
    IReadOnlyList<OpenApiExcludedRoute>? excludedWebRoutes = null,
    OpenApiDocumentInfo? documentInfo = null
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
        // No FastEndpoints auth schemes: its document pass would put every registered scheme on
        // an endpoint with no AuthSchemes. OpenApiOperationSecurity adds the schemes instead.
        options.EnableJWTBearerAuth = false;

        options.ConfigureOpenApi = openApiOptions =>
        {
          // After FastEndpoints' operation transformer, which is registered before this callback.
          // Before OpenApiOneOf: each schema transformer walks the tree on its own, parent first,
          // and OneOf's drops the union's members, so a schema transformer registered after it
          // never reaches the SharedProblemDetails arm or any type inside a response.
          OpenApiProblemDetails.Register(openApiOptions);
          OpenApiTypedId.Register(openApiOptions);
          OpenApiOperationNames.RegisterSchemaDescriptions(openApiOptions);
          OpenApiOneOf.Register(openApiOptions);
          OpenApiDeleteRequestBody.Register(openApiOptions);
          // After OpenApiDeleteRequestBody: the DELETE query parameters it adds can be required.
          OpenApiValidatorRequired.Register(openApiOptions);
          // After OpenApiDeleteRequestBody: the DELETE query parameters it adds get descriptions.
          OpenApiOperationNames.Register(openApiOptions);
          // After OpenApiOneOf: the binary 2xx content replaces the success arm OneOf split out.
          OpenApiBinaryContent.Register(openApiOptions);
          OpenApiOperationSecurity.Register(openApiOptions, agentBearer, sessionCookie);

          OpenApiPublicServer.Register(openApiOptions);
          if (documentPublicWebRoutes)
          {
            OpenApiDiscoveryCoverage.Register(openApiOptions, excludedWebRoutes);
          }

          documentInfo?.Register(openApiOptions);

          configureOpenApi?.Invoke(openApiOptions);
          // After the host callback: operations a host transformer adds are counted.
          OpenApiRootTags.Register(openApiOptions, documentInfo?.TagDescriptions);
          // Last: every transformer that writes a problem response, rewrites a request, or tags an operation has run.
          OpenApiProblemDetails.RegisterDocumentCheck(openApiOptions);
          OpenApiValidatorRequired.RegisterDocumentCheck(openApiOptions);
          OpenApiRootTags.RegisterDocumentCheck(openApiOptions, documentInfo?.TagDescriptions);
          OpenApiOperationNames.RegisterDocumentCheck(openApiOptions);
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
