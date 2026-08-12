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
// Serilog bootstrap logger wraps host build so startup crashes are still captured; the app runs
// through RunOaktonCommands to expose environment checks as CLI commands.
// Web.Spa services are registered here too — prerendering runs SPA code on the server.
// API surface is generated FastEndpoints from [ApiEndpoint] web-contracts (MVC BaseEndpoint
// removed task 131 F-002). Pipeline order: UseMarkdownContentNegotiation (before UseRouting —
// rewrites / → /index.md when Accept prefers text/markdown) → UseTipDiscoveryAlias (before
// UseRouting — bare /api → /api/tip for x402 scanners, task 104-020) → UseRouting →
// UseRateLimiter (task 104-015: path-classified GlobalLimiter for principal-register +
// payment-challenge; after routing so rewrites are settled; edge/Cloudflare is outer ring
// only — 104-023) → UseAuthentication → UseAuthorization → UseAntiforgery (Blazor) →
// UseFastEndpoints → UseScalarApiReference (MapOpenApi + Scalar UI; after FE so endpoint
// metadata is registered). Auth before FE; no FE antiforgery for JSON APIs.
// IncludeAbstractValidators=false — FluentValidationBehavior remains the validation path.
// OpenAPI document: CommonServerModule.AddOpenApi (FastEndpoints.OpenApi, always-on Scalar on web).
// AllowEmptyRequestDtos=true so FE.OpenApi accepts propertyless request DTOs (identity/profile
// empty Queries already use EmptyRequestBinder at runtime).
// Task 145-009 R2-1: ConfigureServices has an explicit-environment 3-arg overload (Main passes
// builder.Environment.EnvironmentName) plus the IModule-required 2-arg overload, which resolves
// the real environment via ResolveRealEnvironmentName instead of IConfiguration — see that
// overload's own Design region for why (a Production-booted host must not activate mock auth from
// config content alone).
// Task 154: identity-session cookie OnRedirectToLogin is dual-mode — HTML/page deep links 302 to
// /Login?returnUrl=…; /api challenges stay 401. Classification SSOT:
// IdentitySessionCookieChallenge (platform/identity-host). Forbid always 403 (never Login).
// Task 183: after Web.Spa.Program.ConfigureServices, re-register AuthenticationStateProvider as
// HostedIdentitySessionAuthenticationStateProvider so prerender uses HttpContext.User (cookie)
// instead of anonymous session loopback — see that type's Design region.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Abuse;
using TimeWarp.Architecture.AgentDiscovery;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Architecture.Features.Profiles.Infrastructure;
using TimeWarp.Architecture.Features.Tip;
using TimeWarp.Foundation.Common.Infrastructure;
using Serilog;

public class Program : IAspNetProgram
{
  const string ApiVersion = "v1";
  const string ApiTitle = $"TimeWarp.Architecture Web.Server API {ApiVersion}";

  public static async Task<int> Main(string[] argumentArray)
  {
    SelfLog.Enable(Console.Error);
    Thread.CurrentThread.Name = nameof(Main);

    Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateBootstrapLogger();

    using ILoggerFactory loggerFactory = new LoggerFactory();
    loggerFactory.AddSerilog(Log.Logger);

    ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

    try
    {
      Log.Information("Starting web host");
      WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);
      builder.Host.UseSerilog((context, services, configuration) =>
        configuration
          .ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

      ConfigureHostApplicationBuilder(builder);
      ConfigureConfiguration(builder.Configuration);
      // Task 145-009 R2-1: pass the REAL host environment explicitly — never let the mock-auth
      // gate re-derive it from IConfiguration (see ConfigureServices' 3-arg overload Design region).
      ConfigureServices(builder.Services, builder.Configuration, builder.Environment.EnvironmentName);

      WebApplication webApplication = builder.Build();

      webApplication.MapDefaultEndpoints();

      Log.Information($"EnvironmentName: {webApplication.Environment.EnvironmentName}");

      ConfigureMiddleware(webApplication);
      ConfigureEndpoints(webApplication);

      return await webApplication.RunOaktonCommands(argumentArray).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      Log.Fatal(exception, messageTemplate: "Host terminated unexpectedly");
      return 1;
    }
    finally
    {
      await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
  }

  private static void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
  {
    builder.AddServiceDefaults();
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
    serviceCollection.AddSerilog();
    serviceCollection.AddHttpClient();
    serviceCollection.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.WebServiceName));
    serviceCollection.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.ApiServiceName));

    serviceCollection
      .AddRazorComponents()
      .AddInteractiveServerComponents()
      .AddInteractiveWebAssemblyComponents();

    serviceCollection.AddCascadingAuthenticationState();
    serviceCollection.AddAuthorizationBuilder()
      // Task 110: any signed-in identity-session cookie — see IdentitySessionDefaults.AuthenticatedPolicy's
      // Design region for why this is deliberately not an admin/role-based policy.
      // Round-1 review M4 (nit): this explicit AddAuthenticationSchemes(IdentitySessionDefaults.Scheme)
      // restriction is WHY roles endpoints get a clean 401 for an unauthenticated request. A bare
      // fail-closed [ApiEndpoint] with no marker at all (only reachable if TWA0013 is suppressed) has
      // no policy to restrict the scheme — it falls through to ASP.NET Core's DEFAULT authentication
      // scheme (identity-session when UseEntra is false; Entra when UseEntra is true — task 104-021).
      // Entra challenges with redirect/500; identity-session cookie events return 401/403. Deny still
      // holds either way; the clean-401 property specifically belongs to an explicit scheme-restricted
      // policy like this one, not to the bare fail-closed default.
      .AddPolicy
      (
        IdentitySessionDefaults.AuthenticatedPolicy,
        policy => policy
          // mock-identity-session (task 145-009): fail-closed header auth for closed-box BFF;
          // handler returns NoResult when mock is off, so Production/normal Dev are unchanged.
          .AddAuthenticationSchemes(IdentitySessionDefaults.Scheme, MockIdentityPrincipalHandler.SchemeName)
          .RequireAuthenticatedUser()
      );
    // Task 182-002/003/006: permission-centric policies (policy name == PermissionIds). Admin,
    // credential, agent identity, and metered-demo contracts use PermissionIds; schemes stay on
    // [EndpointAuthorize(AuthenticationSchemes)]. Agent scope→permission expansion is in
    // PermissionEvaluator (IAgentCallerContext). SPA uses AddPermissionClaimPolicies separately.
    serviceCollection.AddAuthorization(options =>
      PermissionPolicyRegistration.AddPermissionPolicies(options));
    ConfigureAuthentication(serviceCollection, configuration);

    CommonServerModule.ConfigureServices(serviceCollection, configuration);
    ConfigureSettings(serviceCollection, configuration);
    InMemoryIdentityStoresModule.ConfigureServices(serviceCollection, configuration);
    InMemoryProfileStoresModule.ConfigureServices(serviceCollection, configuration);
    CommonInfrastructureModule.ConfigureServices(serviceCollection, configuration);
#if postgres
    PostgresDbModule.ConfigureServices(serviceCollection, configuration);
#endif
    serviceCollection.AddSingleton<IChatHubService, ChatHubService>();
    CorsPolicy.Any.Apply(serviceCollection);
    ConfigureInfrastructure(serviceCollection);
    serviceCollection.AddSignalR();

    serviceCollection.AddHttpContextAccessor();
    serviceCollection.AddScoped<IBrowserSessionService, CookieBrowserSessionService>();
    serviceCollection.AddScoped<IAgentCallerContext, AgentCallerContext>();
    serviceCollection.AddScoped<ICurrentPrincipalAccessor, HttpCurrentPrincipalAccessor>();
    serviceCollection.AddScoped<IRequestHostAccessor, HttpRequestHostAccessor>();
    serviceCollection.AddScoped<IPaymentHttpContext, HttpPaymentHttpContext>();

    // Task 147-004 / 147-006: effective roles + request claims (PrincipalRoleClaimsTransformation
    // still projects roles for diagnostics). Resolver is scoped so it can resolve
    // EfPrincipalRoleStore under postgres without a captive dependency.
    // Task 182-002: PermissionRequirementHandler is the server enforcement path — always via
    // IPermissionEvaluator (scheme-aware expansion of roles→permissions). SPA projects
    // GetCurrentSession.Permissions as claims (182-003).
    serviceCollection.Configure<BootstrapAdministratorOptions>(
      configuration.GetSection("Authentication"));
    serviceCollection.AddScoped<IEffectiveRolesResolver, EffectiveRolesResolver>();
    serviceCollection.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
    serviceCollection.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
    serviceCollection.AddScoped<IClaimsTransformation, PrincipalRoleClaimsTransformation>();

    // TimeWarp.402 demos (104-009 tip, 104-011 metered, 104-013 settle→Funded):
    // in-memory ledger + facilitator + gates. Free/discovery routes never resolve PaymentGate /
    // MeteredCapabilityGate / SettlementFundingService — only tip and metered handlers invoke them.
    // Facilitator base prefers tip options, then metered, then public testnet facilitator.
    // SettlementFundingService + MeteredCapabilityGate are scoped so they can resolve IPrincipalStore
    // when postgres swaps the store to scoped EfPrincipalStore (captive-dependency safe).
    serviceCollection.AddSingleton<ICreditLedger, InMemoryCreditLedger>();
    serviceCollection.AddSingleton<IFacilitatorClient>(static serviceProvider =>
    {
      TipOptions tip = serviceProvider.GetRequiredService<IOptions<TipOptions>>().Value;
      MeteredCapabilityOptions metered = serviceProvider
        .GetRequiredService<IOptions<MeteredCapabilityOptions>>()
        .Value;
      string facilitatorBase =
        !string.IsNullOrWhiteSpace(tip.FacilitatorBase) ? tip.FacilitatorBase
        : !string.IsNullOrWhiteSpace(metered.FacilitatorBase) ? metered.FacilitatorBase
        : FacilitatorUrls.X402Org;
      return new HttpFacilitatorClient(facilitatorBase);
    });
    serviceCollection.AddSingleton<PaymentGate>();
    serviceCollection.AddScoped<SettlementFundingService>();
    serviceCollection.AddScoped<MeteredCapabilityGate>();

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

    // Task 183: last registration wins — prefer cookie principal during hosted prerender.
    // Only when SPA registered IdentitySessionAuthenticationStateProvider (not mock / Entra).
    if (serviceCollection.Any(static d =>
          d.ServiceType == typeof(AuthenticationStateProvider)
          && d.ImplementationType == typeof(IdentitySessionAuthenticationStateProvider)))
    {
      serviceCollection.AddScoped<AuthenticationStateProvider, HostedIdentitySessionAuthenticationStateProvider>();
    }

    serviceCollection
      .AddMediator
      (
        mediatorServiceConfiguration =>
          mediatorServiceConfiguration.RegisterServicesFromAssemblies
          (
            typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.Architecture.Web.Application.IAssemblyMarker).GetTypeInfo().Assembly
          )
      );
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

    CommonServerModule.AddOpenApi(serviceCollection, ApiVersion, ApiTitle);
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

  private static void ConfigureAuthentication(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // Task 104-021: Entra/MSAL is opt-in (Authentication:UseEntra). Default is first-party
    // identity-session cookie as the authentication default scheme — no AzureAd required to boot.
    bool useEntra = MockAuthenticationDefaults.IsEntraAuthActive(
      configuration[MockAuthenticationDefaults.UseEntraKey]);

    AuthenticationBuilder authenticationBuilder;
    if (useEntra)
    {
      // Entra owns the default scheme; identity-session is added as a named scheme via a second
      // parameterless AddAuthentication() (same coexistence model as pre-021).
      serviceCollection.AddMicrosoftIdentityWebAppAuthentication(configuration);
      authenticationBuilder = serviceCollection.AddAuthentication();
    }
    else
    {
      authenticationBuilder = serviceCollection.AddAuthentication(IdentitySessionDefaults.Scheme);
    }

    authenticationBuilder
      .AddCookie(IdentitySessionDefaults.Scheme, options =>
      {
        options.Cookie.Name = IdentitySessionDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        // Dual-mode challenge (task 154): non-API HTML/page deep links 302 to /Login?returnUrl=…
        // so the SPA and task-153 client flow can run; /api/… stays 401 (contract seam). Forbid
        // is always 403 — authenticated-but-insufficient policy must not bounce to Login.
        // Classification SSOT: IdentitySessionCookieChallenge.
        options.Events.OnRedirectToLogin = context =>
        {
          if (IdentitySessionCookieChallenge.ShouldRedirectToLogin(context.Request))
          {
            context.Response.Redirect(
              IdentitySessionCookieChallenge.BuildLoginRedirectTarget(context.Request));
          }
          else
          {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          }

          return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          return Task.CompletedTask;
        };
      })
      // Agent bearer-token scheme (task 104-004): named scheme alongside identity-session.
      // AgentTokenAuthenticationHandler owns authenticate/challenge/forbid for this scheme;
      // token lifetime lives in AgentTokenOptions (ConfigureSettings).
      .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AgentTokenDefaults.Scheme, _ => { })
      // Closed-box mock principal (task 145-009): always registered; handler is fail-closed
      // (Development/Testing + Authentication:UseMock + header). Listed on AuthenticatedPolicy.
      .AddScheme<AuthenticationSchemeOptions, MockIdentityPrincipalHandler>(MockIdentityPrincipalHandler.SchemeName, _ => { });
  }

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
      webApplication.UseWebAssemblyDebugging();
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

    // Identity session (task 104-003): named cookie scheme only — the dormant Entra registration's
    // own auth flow is untouched. Ceremony endpoints (register/authenticate) are anonymous by
    // design (they establish the session); GetCurrentSession reads whatever session exists, if any.
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
    webApplication.MapStaticAssets();
    webApplication.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode()
      .AddInteractiveWebAssemblyRenderMode()
      .AddAdditionalAssemblies
      (
        typeof(TimeWarp.State.AssemblyMarker).Assembly,
        typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly
      );

    webApplication.MapHealthChecks("/api/health");

    CommonServerModule.ConfigureEndpoints(webApplication);
    webApplication.MapHub<ChatHub>(ChatHubConstants.Route);

    // Map the new endpoint to expose service discovery information
    webApplication.MapGet
    (
      "/service-discovery",
      async context =>
      {
        var services = new Dictionary<string, Uri?>
        {
          { TimeWarp.Foundation.Configuration.ServiceNames.GrpcServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.GrpcServiceName) },
          { TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName) },
          { TimeWarp.Foundation.Configuration.ServiceNames.WebServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.WebServiceName) }
        };

        await context.Response.WriteAsJsonAsync(services);
      }
    );
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection
      .AddFluentValidatedOptions<SampleOptions, SampleOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<WebAuthnOptions, WebAuthnOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<AgentTokenOptions, AgentTokenOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<MeteredCapabilityOptions, MeteredCapabilityOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<TipOptions, TipOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<AbuseRateLimitOptions, AbuseRateLimitOptionsValidator>(configuration)
      .ValidateOnStart();

    // TIP_* env overlay (timewarp-software parity) after section bind; strict TIP_ENABLED=="true".
    serviceCollection.PostConfigure<TipOptions>(static options => TipEnvironment.ApplyFromEnvironment(options));
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHealthChecks();

    ConfigureEnvironmentChecks(serviceCollection);
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<SampleEnvironmentCheck>();

    serviceCollection.CheckEnvironment<SampleEnvironmentCheck>
    (
      SampleEnvironmentCheck.Description, sampleEnvironmentCheck => sampleEnvironmentCheck.Check()
    );
  }
}
