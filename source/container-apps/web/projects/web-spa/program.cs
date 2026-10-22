#region Purpose
// Blazor WebAssembly entry point: composes auth, TimeWarp.State, and API services for the SPA.
#endregion

#region Design
// ConfigureServices is public static so integration tests and Web.Server prerender compose the
// same container as the app. Auth is runtime-config-gated (tasks 145-009 + RFC 219 D10):
//   1. Development/Testing + Authentication:UseMock → MockAuthenticationRegistration
//   2. else IdentitySessionAuthenticationRegistration (passkey cookie via GetCurrentSession)
// Named Entra is a BFF challenge, not a WASM MSAL session. Fail-closed: Production never activates
// mock even when UseMock is true. optional MOCK_WEB_API still compile-time for
// offline SPA API fakes. Template symbols (api, grpc) trim optional services. API services use
// explicit factories so DI does not guess constructors. Default culture is forced to ISO date
// patterns for deterministic rendering. SetIsoCulture hardcodes en-US: Profile.Language is a
// stored preference only (task 205-002). Missing UI resources fall back to English until a
// later i18n task ships translations and wires DefaultThreadCurrentUICulture.
//
// Task 145-009 R2-1 fix: ConfigureServices takes environmentName as a REQUIRED explicit
// parameter — there is deliberately no config-derived overload. An earlier 2-arg overload used to
// fall back to configuration["ASPNETCORE_ENVIRONMENT"] ?? ["DOTNET_ENVIRONMENT"], which reads
// IConfiguration content, NOT the real IHostEnvironment: on a genuinely Production-booted host
// (real env var unset), any later-loaded config provider (appsettings, CLI args, …) setting either
// key activated mock auth — proven dynamically in round-2 review. Every caller now passes the real
// environment: this file's own Main passes builder.HostEnvironment.Environment (WASM host); Web.Server
// resolves the true IHostEnvironment (never IConfiguration) and passes it in explicitly — see
// Web.Server.Program's ConfigureServices Design region.
// Task 205-003: WASM named HttpClients attach BrowserRequestCredentialsHandler (fetch
// credentials SameOrigin) so the identity-session cookie rides on PUT/GET to the SPA origin.
// Task 312: every WASM named client targets the origin the SPA was served from (the ingress on
// every target). The gRPC client adds IngressPaths.GrpcPathPrefix; the browser never receives
// internal service URIs (the /service-discovery endpoint and ServiceUriProvider are gone).
//
// No new template.json feature flag for identity/x402 (they ship with the template); Entra is a
// runtime config switch, not a compile-time DefineConstants symbol (avoids TWA0008/0010 dual paths).
//
// TimeWarp.State 12.0.0-beta.15 registers Blazor services with AddTimeWarpStateBlazor.
// Call it after AddTimeWarpState so RenderSubscriptionsPostProcessor can be constructed.
// Task 278: JavaScript dispatch is opt-in since TimeWarp.State 12.0.0-beta.8. AllowJavaScriptDispatch
// is the allow-list (today only the Counter demo's IncrementCounter, under the
// IncrementCounterActionSet.JavaScriptAlias wire name so counter.ts never spells the CLR type name);
// it is public so SPA test hosts register the same list instead of copying it.
// Task 335-002: AddStateDevToolsExportAsync (Main only, never ConfigureServices: web-server prerender
// has its own server exporter) posts browser store spans to the State DevTools inspector. In
// Development it asks web-server for the inspector origin (GET /_timewarp/state-devtools, mapped by
// Web.Server.Program.ConfigureEndpoints) and registers the exporter only when an origin resolves;
// snapshots turn on only then. The package reference is Debug-only and the call compiles only with
// the DEBUG symbol, so a Release build or publish has neither the assembly nor the call. Each DEBUG
// block sits inside the cnd:noEmit escape so dotnet new keeps it: DEBUG is not a template symbol.
// Snapshots use a reflection resolver: Debug WebAssembly is not trimmed, and a failed snapshot never
// fails the action. A Development Debug start without an inspector costs one discovery GET (204).
#endregion

namespace TimeWarp.Architecture.Web.Spa;

using System.Globalization;
using TimeWarp.Features.JavaScriptInterop;
//-:cnd:noEmit
#if DEBUG
using System.Text.Json.Serialization.Metadata;
using TimeWarp.State.DevTools.Wasm;
using TimeWarp.State.Telemetry;
#endif
//+:cnd:noEmit

public static class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    SetIsoCulture();
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    ConfigureServices(builder.Services, builder.Configuration, builder.HostEnvironment.Environment);
//-:cnd:noEmit
#if DEBUG
    await AddStateDevToolsExportAsync(builder);
#endif
//+:cnd:noEmit
    builder.Services.AddTransient<BrowserRequestCredentialsHandler>();
    builder.Services.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
      .AddHttpMessageHandler<BrowserRequestCredentialsHandler>();
#if api
    builder.Services.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
      .AddHttpMessageHandler<BrowserRequestCredentialsHandler>();
#endif
#if grpc
    // Task 312: gRPC-Web goes through the ingress origin; the ingress strips the prefix.
    builder.Services.AddHttpClient(ServiceNames.GrpcServiceName, client => client.BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), $"{IngressPaths.GrpcPathPrefix.TrimStart('/')}/"));
#endif

    await using WebAssemblyHost host = builder.Build();
    await host.RunAsync();
  }

//-:cnd:noEmit
#if DEBUG
  /// <summary>
  /// Task 335-002: exports browser TimeWarp.State action spans to the State DevTools inspector.
  /// Development only, and only when web-server's discovery route returns an origin (the AppHost set
  /// TIMEWARP_STATE_DEVTOOLS_ENDPOINT in run mode). Otherwise nothing is registered.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Debug-only DevTools snapshots; Debug WebAssembly is not trimmed.")]
  [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Debug-only DevTools snapshots; Debug WebAssembly is not AOT-compiled.")]
  private static async Task AddStateDevToolsExportAsync(WebAssemblyHostBuilder builder)
  {
    string? endpoint = await TimeWarpStateDevToolsWasm.ResolveEndpointAsync
    (
      builder.HostEnvironment.IsDevelopment(),
      builder.Configuration[TimeWarpStateDevToolsWasm.EndpointConfigurationKey],
      builder.HostEnvironment.BaseAddress
    );
    if (endpoint is null)
    {
      return;
    }

    builder.Services.AddTimeWarpStateTelemetry
    (
      options =>
      {
        options.IncludeSnapshots = true;
        options.JsonSerializerOptions =
          new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
      }
    );
    builder.Services.AddTimeWarpStateDevToolsWasmExporter(endpoint);
  }
#endif
//+:cnd:noEmit

  private static void SetIsoCulture()
  {
    // English UI fallback: do not read Profile.Language here. Stored locale recognition is
    // independent of applied UI culture until i18n resources exist for that tag.
    var isoCulture =
      new CultureInfo("en-US")
      {
        DateTimeFormat =
        {
          ShortDatePattern = "yyyy-MM-dd", LongDatePattern = "yyyy-MM-ddTHH:mm:ss"
        }
      };

    CultureInfo.DefaultThreadCurrentCulture = isoCulture;
    CultureInfo.DefaultThreadCurrentUICulture = isoCulture;
  }

  /// <summary>
  /// The actions JavaScript may dispatch through timeWarpState.DispatchRequest.
  /// </summary>
  public static void AllowJavaScriptDispatch(JavaScriptDispatchBuilder javaScriptDispatchBuilder) =>
    javaScriptDispatchBuilder.Allow<CounterState.IncrementCounterActionSet.Action>(CounterState.IncrementCounterActionSet.JavaScriptAlias);

  /// <summary>
  /// Compose SPA services. <paramref name="environmentName"/> drives the fail-closed mock-auth
  /// gate (Development/Testing + Authentication:UseMock) and MUST be the caller's real
  /// <see cref="IHostEnvironment"/> EnvironmentName (or WebAssembly HostEnvironment.Environment) —
  /// never a value read back out of <paramref name="configuration"/> (task 145-009 R2-1: a
  /// config-derived environment can diverge from the real host environment and was a fail-open
  /// bug). Unknown/absent (null/empty) is fail-closed — mock auth is never activated.
  /// </summary>
  public static void ConfigureServices
  (
    IServiceCollection serviceCollection,
    IConfiguration configuration,
    string? environmentName
  )
  {
    if (!MockAuthenticationRegistration.TryAddSpaMockAuthentication(serviceCollection, configuration, environmentName))
    {
      // Non-mock path is always identity-session. Entra is a BFF named-scheme challenge
      // (RFC 219 D10); WASM MSAL is not the session.
      IdentitySessionAuthenticationRegistration.AddSpaIdentitySessionAuthentication(serviceCollection);
    }

    // SPA permission claim policies (PermissionIds) + Anonymous/Authenticated.
    serviceCollection.AddAuthorizationCore(PolicyRegistration.AddPolicies);
    serviceCollection.AddFluentUIComponents();
    // FilterBar's search debounce reads this clock. Tests register their own TimeProvider.
    serviceCollection.AddSingleton(TimeProvider.System);
    serviceCollection.AddBlazoredSessionStorage();
    serviceCollection.AddBlazoredLocalStorage();

    ConfigureSettings(serviceCollection, configuration);
    // AddTimeWarpState no longer registers a mediator. State 12.0.0-beta.3 scopes store
    // handlers to ClientPipeline; this host owns the generator call.
    serviceCollection.AddWebSpaGeneratedMediator();
    serviceCollection.AddTimeWarpState
    (
      timeWarpStateOptions =>
      {
        timeWarpStateOptions.Assemblies =
          new[]
          {
            // ReSharper disable once RedundantNameQualifier
            typeof(Web.Spa.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );
    serviceCollection.AddTimeWarpStateBlazor();

    // Opt-in [CatalogAction] registry (TimeWarp.State 12.0.0-beta.7) for the Ctrl-K palette and
    // agent tools. Only web-spa declares cataloged actions; Plus declares none, so it is not named.
    serviceCollection.AddActionCatalog(typeof(Web.Spa.IAssemblyMarker).GetTypeInfo().Assembly);
    // Ask is always registered. RelayChatClient holds no key; web-server owns XAI:ApiKey.
    // WebMCP uses the same catalog tools and does not need this client.
    serviceCollection.AddScoped<PageAgentRoute>();
    serviceCollection.AddScoped<AgentCallOutcome>();
    serviceCollection.AddScoped<WebMcpApprovalGate>();
    serviceCollection.AddScoped<WebMcpDispatcher>();
    serviceCollection.AddScoped<JsWebMcpModelContext>();
    serviceCollection.AddScoped<WebMcpPublisher>();
    // The Ask transcript outlives the panel and page remounts; one thread per conversation generation.
    serviceCollection.AddScoped<AskConversationThreads>();
    // Task 361: the derived Ctrl-K Ask row reads "Ask {AssistantName}".
    serviceCollection.AddScoped<IActionDisplayOverride, AskAssistantDisplayOverride>();
    serviceCollection.AddJavaScriptDispatch(AllowJavaScriptDispatch);

    // Plus [assembly: MediatorAssembly] links LoadPersistentStateRequestHandler and
    // StateInitializedNotificationHandler, which require IPersistenceService.
    serviceCollection.AddTimeWarpStatePersistence();

    // Form validation uses Blazilla (FluentValidation for EditForm). Components pass an explicit
    // validator instance (e.g. <FluentValidator Validator="new RoleDetailsValidator()"/>), so no
    // DI registration is required here. (Replaced the deprecated Blazored / unwired Morris path.)

    serviceCollection.AddScoped<ChatHubConnection>();
    serviceCollection.AddScoped<PasskeyCeremonyClient>();
    // Clears the shell notification region on route change (task 247 rule 4). Activated by
    // Routes.razor's @inject; composed into web-server prerender through this same method.
    serviceCollection.AddScoped<NotificationState.NavigationListener>();

    // We are using a factory here to explicitly determine which constructor to use for DI.
    serviceCollection.AddScoped<IWebServerApiService>
    (
      serviceProvider =>
      {
        IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        IOptions<JsonSerializerOptions> options = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();
        var realService = new WebServerApiService(accessTokenProvider, httpClientFactory, options);
#if MOCK_WEB_API
        ILogger<MockWebApiService> logger = serviceProvider.GetRequiredService<ILogger<MockWebApiService>>();
        return new MockWebApiService(realService, logger, serviceProvider);
#else
        return realService; // Comment out to use the mock service
#endif
      }
    );

    // Unkeyed IChatClient is the browser relay. The xAI client is keyed on the server only.
    serviceCollection.AddScoped<IChatClient, RelayChatClient>();

    // We are using a factory here to explicitly determine which constructor to use for DI.
#if api
    serviceCollection.AddScoped<IApiServerApiService>
    (
      serviceProvider =>
      {
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
        IOptions<JsonSerializerOptions> options = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        return new ApiServerApiService(httpClientFactory, accessTokenProvider, options);
      }
    );
#endif

    // Set the JSON serializer options
    // Contract-seam serialization is declared once in ContractSerializationDefaults.
    serviceCollection.Configure<JsonSerializerOptions>(ContractSerializationDefaults.Apply);

#if grpc
    SuperheroModule.ConfigureServices(serviceCollection, configuration);
#endif
    serviceCollection.AddSingleton(serviceCollection);
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection
      .AddFluentValidatedOptions<BlazorSettings, BlazorSettingsValidator>(configuration)
      .ValidateOnStart();
  }
}
