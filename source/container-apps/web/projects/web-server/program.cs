#region Purpose
// Composition root for Web.Server: hosts the Blazor SPA (Server + WebAssembly interactivity), its API endpoints, and the chat hub.
#endregion

#region Design
// ConfigureConfiguration/ConfigureServices/ConfigureMiddleware/ConfigureEndpoints are public
// statics (IAspNetProgram) so integration-test hosts compose the exact production pipeline.
// Cross-cutting registrations delegate to modules (CommonServerModule etc.); the PostgresDbModule
// call is gated by the postgres template feature flag via a preprocessor directive, so a template
// consumer without that flag compiles the call out entirely. When the flag is present the module
// itself still no-ops at runtime if no connection string is configured (see PostgresDbModule).
// Logging is the host ILogger plus AddOpenTelemetry from AddServiceDefaults (same story as
// api-server). Do not bootstrap Serilog: UseSerilog replaces host providers and drops the OTLP
// logging exporter, so structured logs never reach the Aspire dashboard. Startup crash capture is
// a try/catch around host build/run — ILogger after Build, Console.Error before any logger exists.
// The host runs with RunAsync and returns 0 after a clean shutdown (1 when startup throws).
// Reachability is the ASP.NET health checks: ServiceDefaults' live check, and
// AddDbContextCheck<PostgresDbContext> when a Postgres connection is configured.
// Web.Spa services are registered here too — prerendering runs SPA code on the server.
// API surface is generated FastEndpoints from [ApiEndpoint] web-contracts (MVC BaseEndpoint
// removed task 131 F-002). Pipeline order: UseMarkdownContentNegotiation (before UseRouting —
// rewrites / → /index.md when Accept prefers text/markdown) → UseTipDiscoveryAlias (before
// UseRouting — bare /api → /api/tip for x402 scanners, task 104-020) → UseRouting →
// UseRateLimiter (task 104-015: path-classified GlobalLimiter for principal-register +
// payment-challenge; after routing so rewrites are settled; edge/Cloudflare is outer ring
// only — 104-023) → RoleResolutionFailureMiddleware (task 160: inner of DeveloperExceptionPage,
// before UseAuthentication, maps RoleResolutionFailedException to 503) → UseAuthentication →
// UseAuthorization → UseAntiforgery (Blazor) → UseFastEndpoints → UseScalarApiReference
// (MapOpenApi + Scalar UI; after FE so endpoint metadata is registered). Auth before FE; no FE
// antiforgery for JSON APIs.
// IncludeAbstractValidators=false — FluentValidationBehavior remains the validation path.
// OpenAPI document: CommonServerModule.AddOpenApi (FastEndpoints.OpenApi, always-on Scalar on web).
// OpenApiAgentReviewerGuide appends the token steps and the AgentTokenOptions lifetime onto
// JWTBearerAuth when that document is built.
// documentPublicWebRoutes: this host serves /api/health, the /api tip alias, and the SPA
// discovery documents. The OpenAPI note is how an agent learns a missing path is an exclusion.
// AllowEmptyRequestDtos=true so FE.OpenApi accepts propertyless request DTOs (identity/profile
// empty Queries already use EmptyRequestBinder at runtime).
// Task 145-009 R2-1: ConfigureServices has an explicit-environment 3-arg overload (Main passes
// builder.Environment.EnvironmentName) plus the IModule-required 2-arg overload, which resolves
// the real environment via ResolveRealEnvironmentName instead of IConfiguration — see that
// overload's own Design region for why (a Production-booted host must not activate mock auth from
// config content alone).
// Registration concerns live in their own types, each with its own Design region:
// WebServerHttpClients (service-discovery named clients, task 312), WebServerAuthentication
// (schemes, policies, permission evaluation, the hosted prerender session state),
// WebServerSettings (validated options), WebServerPaymentServices (TimeWarp.402 demos),
// WebServerStateDevTools (server-side span export) and BrandStylesheetEndpoint. ConfigureServices
// keeps the registration order: the hosted session state goes after Web.Spa.Program.ConfigureServices
// because the last AuthenticationStateProvider registration wins.
// Task 335-002: browser (WASM) stores export from Web.Spa.Program.Main. The browser never sees the
// variable, so ConfigureEndpoints maps MapTimeWarpStateDevToolsClientConfig: GET
// /_timewarp/state-devtools returns the inspector origin, 204 when the variable is unset. The package
// maps it in Development only; every other environment has no route. The ingress catch-all reaches it.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using TimeWarp.Architecture.Abuse;
using TimeWarp.Architecture.AgentDiscovery;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Architecture.Features.AgentLinks.Infrastructure;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;
using TimeWarp.Architecture.Features.Profiles.Infrastructure;
using TimeWarp.Architecture.Mail;
using TimeWarp.Architecture.Features.Tip;
using TimeWarp.Foundation.Common.Infrastructure;
using TimeWarp.State.DevTools.Export;

public partial class Program : IAspNetProgram
{
  const string ApiVersion = "v1";
  const string ApiTitle = $"TimeWarp.Architecture Web.Server API {ApiVersion}";

  public static async Task<int> Main(string[] argumentArray)
  {
    Thread.CurrentThread.Name = nameof(Main);

    ILogger<Program>? logger = null;
    try
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);

      ConfigureHostApplicationBuilder(builder);
      ConfigureConfiguration(builder.Configuration);
      // Task 145-009 R2-1: pass the REAL host environment explicitly — never let the mock-auth
      // gate re-derive it from IConfiguration (see ConfigureServices' 3-arg overload Design region).
      ConfigureServices(builder.Services, builder.Configuration, builder.Environment.EnvironmentName);

      WebApplication webApplication = builder.Build();
      logger = webApplication.Services.GetRequiredService<ILogger<Program>>();

      webApplication.MapDefaultEndpoints();

      LogStartingWebHost(logger);
      LogEnvironmentName(logger, webApplication.Environment.EnvironmentName);

      ConfigureMiddleware(webApplication);
      ConfigureEndpoints(webApplication);

      await webApplication.RunAsync().ConfigureAwait(false);
      return 0;
    }
    catch (Exception exception)
    {
      if (logger is not null)
      {
        LogHostTerminatedUnexpectedly(logger, exception);
      }
      else
      {
        await Console.Error.WriteLineAsync($"Host terminated unexpectedly{Environment.NewLine}{exception}").ConfigureAwait(false);
      }

      return 1;
    }
  }

  private static void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
  {
    builder.AddServiceDefaults();
    WebServerStateDevTools.AddExport(builder);
  }

  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    CommonServerModule.ConfigureConfiguration(configurationManager);
  }

  /// <summary>
  /// IModule-required overload (generic callers — e.g. WebApplicationHost&lt;TProgram&gt; test
  /// harness — cannot pass an explicit environment name through the static-interface contract).
  /// Resolves the REAL host environment from the not-yet-built <paramref name="serviceCollection"/>
  /// (see <see cref="ResolveRealEnvironmentName"/>) rather than IConfiguration — never derived,
  /// never spoofable by a later-added config source.
  /// </summary>
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration) =>
    ConfigureServices(serviceCollection, configuration, ResolveRealEnvironmentName(serviceCollection));

  /// <summary>
  /// Explicit-environment overload (task 145-009 R2-1 fix). <paramref name="environmentName"/> MUST
  /// be the real <see cref="IHostEnvironment.EnvironmentName"/> — Main passes
  /// <c>builder.Environment.EnvironmentName</c> directly. This is the sole gate input threaded into
  /// <see cref="Web.Spa.Program.ConfigureServices(IServiceCollection, IConfiguration, string?)"/>'s
  /// fail-closed mock-auth check; Web.Spa no longer offers a config-derived fallback (removed —
  /// IConfiguration content alone must never be able to activate mock auth on a Production-booted
  /// host, since providers loaded after host creation can set arbitrary key values).
  /// </summary>
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration, string? environmentName)
  {
    WebServerHttpClients.ConfigureServices(serviceCollection);

    serviceCollection
      .AddRazorComponents()
      .AddInteractiveServerComponents()
      .AddInteractiveWebAssemblyComponents();

    WebServerAuthentication.ConfigureServices(serviceCollection, configuration);

    CommonServerModule.ConfigureServices(serviceCollection, configuration);
    WebServerSettings.ConfigureServices(serviceCollection, configuration);
    InMemoryIdentityStoresModule.ConfigureServices(serviceCollection, configuration);
    // Scoped: ISiteSettingsStore is the scoped SeedOnReadSiteSettingsStore decorator (task 254).
    serviceCollection.AddScoped<IEntraSignInPolicy, SiteSettingsEntraSignInPolicy>();
    InMemoryProfileStoresModule.ConfigureServices(serviceCollection, configuration);
    InMemoryFeedbackStoresModule.ConfigureServices(serviceCollection, configuration);
    FeedbackAttachmentBlobModule.ConfigureServices(serviceCollection, configuration);
    MailModule.ConfigureServices(serviceCollection, configuration);
    InMemoryAgentHumanLinkStoresModule.ConfigureServices(serviceCollection, configuration);
    CommonInfrastructureModule.ConfigureServices(serviceCollection, configuration);
#if postgres
    PostgresDbModule.ConfigureServices(serviceCollection, configuration);
#endif
    // Task 254: after the store backend is final — wraps it so an empty store seeds on first read.
    SiteSettingsSeedRegistration.ConfigureServices(serviceCollection);
    serviceCollection.AddSingleton<IChatHubService, ChatHubService>();
    serviceCollection.AddSingleton<BrowserLogRateLimiter>();
    CorsPolicy.Any.Apply(serviceCollection);
    ConfigureInfrastructure(serviceCollection);
    serviceCollection.AddSignalR();

    serviceCollection.AddHttpContextAccessor();
    serviceCollection.AddScoped<IBrowserSessionService, CookieBrowserSessionService>();
    serviceCollection.AddScoped<IEntraChoiceTicketAccessor, HttpEntraChoiceTicketAccessor>();
    serviceCollection.AddScoped<IAgentCallerContext, AgentCallerContext>();
    serviceCollection.AddScoped<IAgentPermissionScopeSource, AgentCallerPermissionScopeSource>();
    serviceCollection.AddScoped<ICurrentPrincipalAccessor, HttpCurrentPrincipalAccessor>();
    serviceCollection.AddScoped<IRequestHostAccessor, HttpRequestHostAccessor>();
    serviceCollection.AddScoped<IRequestUserAgentAccessor, HttpRequestUserAgentAccessor>();
    serviceCollection.AddScoped<IPaymentHttpContext, HttpPaymentHttpContext>();

    WebServerAuthentication.ConfigurePermissionEvaluation(serviceCollection, configuration);

    // TimeWarp.402 demos (104-009 tip, 104-011 metered, 104-013 settle→Funded).
    WebServerPaymentServices.ConfigureServices(serviceCollection);

    // Task 104-015: app-level rate limits on principal register + payment challenge (not edge).
    AbuseRateLimitingModule.ConfigureServices(serviceCollection, configuration);

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Server.IAssemblyMarker>();
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Contracts.IAssemblyMarker>();

    serviceCollection.AddFastEndpoints(options =>
    {
      // FluentValidationBehavior (mediator) owns validation — do not auto-wire FE validators.
      options.IncludeAbstractValidators = false;
      // ApplicationName is web-server (the endpoint assembly). Without DisableAutoDiscovery, FE
      // would scan that assembly automatically AND again via Assemblies → duplicate routes.
      options.DisableAutoDiscovery = true;
      options.Assemblies =
      [
        typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly
      ];
    });

    serviceCollection.AddResponseCompression
    (
      responseCompressionOptions =>
        responseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
        (
          new[]
          {
            MediaTypeNames.Application.Octet
          }
        )
    );

    Web.Spa.Program.ConfigureServices(serviceCollection, configuration, environmentName);

    // Task 289: key-bearing xAI client. Not the unkeyed IChatClient — SPA registers that relay
    // above, and prerender resolves the same container. A missing key does not fail startup.
    XaiChatRegistration.ConfigureServices(serviceCollection, configuration);

    // Task 183: after Web.Spa.Program.ConfigureServices — last registration wins.
    WebServerAuthentication.PreferHostedIdentitySessionState(serviceCollection);

    // Unique wrapper: AddGeneratedMediator is internal to the emitting assembly, and web-server
    // references Web.Spa, so this host calls the public wrapper instead of the generated method.
    serviceCollection.AddWebApplicationGeneratedMediator();

    CommonServerModule.AddOpenApi
    (
      serviceCollection,
      ApiVersion,
      ApiTitle,
      OpenApiAgentBearerRegistration.Create(),
      documentPublicWebRoutes: true,
      configureOpenApi: options =>
      {
        OpenApiPaymentResponses.Register(options);
        OpenApiAgentReviewerGuide.Register(options);
      }
    );
  }

  /// <summary>
  /// Resolves the REAL ASP.NET Core host environment from a not-yet-built
  /// <paramref name="serviceCollection"/> (task 145-009 R2-1). WebApplicationBuilder pre-registers a
  /// singleton <see cref="IHostEnvironment"/> instance (builder.Environment) into
  /// <c>builder.Services</c> at host-builder-creation time, BEFORE ConfigureServices runs and BEFORE
  /// Build() — that instance is fixed then and is never mutated by configuration providers added
  /// afterward, unlike <c>configuration["ASPNETCORE_ENVIRONMENT"]</c>/<c>["DOTNET_ENVIRONMENT"]</c>,
  /// which any later config source (appsettings, CLI args, env vars processed differently, …) can
  /// freely set. Only the IModule-required 2-arg <see cref="ConfigureServices(IServiceCollection, IConfiguration)"/>
  /// overload needs this — direct callers (Main) pass builder.Environment.EnvironmentName explicitly.
  /// Fail-closed: returns null (never mock-eligible — see MockAuthenticationDefaults.IsMockEnvironmentAllowed)
  /// if the descriptor is unexpectedly absent.
  /// </summary>
  private static string? ResolveRealEnvironmentName(IServiceCollection serviceCollection) =>
    (serviceCollection.LastOrDefault(static descriptor => descriptor.ServiceType == typeof(IHostEnvironment))
      ?.ImplementationInstance as IHostEnvironment)?.EnvironmentName;

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Although sometimes, you might want to allow other sites to make cross-origin requests to your app to be functional.
    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
      webApplication.UseDeveloperExceptionPage();
      // .NET 11 retired UseWebAssemblyDebugging (ASPDEPR011). Visual Studio and VS Code
      // launch Blazor WebAssembly debugging directly; no inspectUri proxy remains.
    }

    webApplication.UseResponseCompression();
    // Agent-ready markdown twins (task 104-018): must run before UseRouting so endpoint matching
    // sees /index.md (etc.) when Accept prefers text/markdown. Browsers omit text/markdown and
    // fall through to Blazor; MapStaticAssets serves the twin with Content-Type: text/markdown.
    webApplication.UseMarkdownContentNegotiation();
    // x402 commerce scanners (task 104-020): bare /api → /api/tip before UseRouting so the tip
    // FastEndpoint matches; challenge Resource stays /api/tip (TipOptions). Exact path only —
    // /api/health and other /api/* routes are untouched. Free/discovery paths never rewrite.
    webApplication.UseTipDiscoveryAlias();
    // Static assets (including the Blazor WASM framework files) are served exclusively by
    // MapStaticAssets in ConfigureEndpoints. Do not add UseBlazorFrameworkFiles or UseStaticFiles:
    // UseBlazorFrameworkFiles' MapWhen branch 404s the dynamic /_framework/resource-collection.*.js
    // endpoint required by WebAssembly interactivity, and UseStaticFiles bypasses the fingerprinted
    // caching headers.
    webApplication.UseRouting();

    // Task 104-015: after UseRouting (rewrites settled); before FE so rejected requests never
    // reach handlers / PaymentGate. Path-classified GlobalLimiter + structured 429 OnRejected.
    // Edge volumetric limits stay outside the app (104-023).
    webApplication.UseRateLimiter();

    // Task 160: inner of UseDeveloperExceptionPage (registered above in Development) so a
    // RoleResolutionFailedException from IClaimsTransformation becomes 503, not the Dev page's
    // 500 and not cookie Challenge 401. Production has no Dev page; this is the mapper.
    webApplication.UseMiddleware<RoleResolutionFailureMiddleware>();

    // Identity session (task 104-003 / RFC 219 D10): default cookie scheme. Named entra OIDC is a
    // challenge-only scheme and never DefaultScheme. Ceremony endpoints (register/authenticate) are
    // anonymous by design (they establish the session); GetCurrentSession reads whatever session
    // exists, if any.
    webApplication.UseAuthentication();
    webApplication.UseAuthorization();

    // Blazor antiforgery for interactive components — not applied to FastEndpoints JSON APIs.
    webApplication.UseAntiforgery();

    webApplication.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      // Empty request DTOs (propertyless Query/Command + EmptyRequestBinder) are first-class;
      // FE.OpenApi otherwise throws when generating /openapi/*.json for those endpoints.
      config.Endpoints.AllowEmptyRequestDtos = true;
    });

    // OpenAPI document + Scalar UI require FastEndpoints registration first (always-on on web).
    CommonServerModule.UseScalarApiReference(webApplication, ApiVersion, ApiTitle);
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    BrandStylesheetEndpoint.Map(webApplication);

    webApplication.MapStaticAssets();
    WebMcpDiscoveryEndpoint.Map(webApplication);
    webApplication.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode()
      .AddInteractiveWebAssemblyRenderMode()
      .AddAdditionalAssemblies
      (
        // TimeWarp.State.Blazor ships the Razor components (TimeWarpJavaScriptInterop). TimeWarp.State
        // has no Razor components.
        typeof(TimeWarp.State.Blazor.AssemblyMarker).Assembly,
        typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly
      );

    // Anonymous on purpose: the OpenAPI health operation says so, and a fallback policy must not hide it.
    webApplication.MapHealthChecks(TimeWarp.Foundation.Configuration.IngressPaths.WebServerHealthPath).AllowAnonymous();

    CommonServerModule.ConfigureEndpoints(webApplication);
    webApplication.MapHub<ChatHub>(ChatHubConstants.Route);
    // Task 335-002: Development-only discovery route the WASM DevTools exporter reads (see Design).
    webApplication.MapTimeWarpStateDevToolsClientConfig();
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHealthChecks();
  }

  [LoggerMessage(Level = LogLevel.Information, Message = "Starting web host")]
  private static partial void LogStartingWebHost(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EnvironmentName: {EnvironmentName}")]
  private static partial void LogEnvironmentName(ILogger logger, string environmentName);

  [LoggerMessage(Level = LogLevel.Critical, Message = "Host terminated unexpectedly")]
  private static partial void LogHostTerminatedUnexpectedly(ILogger logger, Exception exception);
}

