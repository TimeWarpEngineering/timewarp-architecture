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
//
// No new template.json feature flag for identity/x402 (they ship with the template); Entra is a
// runtime config switch, not a compile-time DefineConstants symbol (avoids TWA0008/0010 dual paths).
#endregion

namespace TimeWarp.Architecture.Web.Spa;

using System.Globalization;

public static class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    SetIsoCulture();
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    ConfigureServices(builder.Services, builder.Configuration, builder.HostEnvironment.Environment);
    builder.Services.AddTransient<BrowserRequestCredentialsHandler>();
    builder.Services.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
      .AddHttpMessageHandler<BrowserRequestCredentialsHandler>();
#if api
    builder.Services.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
      .AddHttpMessageHandler<BrowserRequestCredentialsHandler>();
#endif

    await using WebAssemblyHost host = builder.Build();
    await host.RunAsync();
  }

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
        // Always register. TimeWarp.State's generated mediator links CommitHandler, which
        // requires ReduxDevToolsInterop / IReduxDevToolsStore / ReduxDevToolsOptions.
        // Development ValidateOnBuild (in-proc test hosts and `dotnet run` Development)
        // fails without them. The <ReduxDevTools/> component and InitAsync stay
        // ReduxDevToolsEnabled (Debug) so Release prerender does not render the
        // component; without InitAsync, Interop.IsEnabled stays false and dispatch is a
        // no-op.
        timeWarpStateOptions.UseReduxDevTools(reduxDevToolsOptions => reduxDevToolsOptions.Trace = false);

        timeWarpStateOptions.Assemblies =
          new[]
          {
            // ReSharper disable once RedundantNameQualifier
            typeof(Web.Spa.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );

    // Plus [assembly: MediatorAssembly] links LoadPersistentStateRequestHandler and
    // StateInitializedNotificationHandler, which require IPersistenceService.
    serviceCollection.AddScoped<TimeWarp.Features.Persistence.IPersistenceService, TimeWarp.Features.Persistence.PersistenceService>();

    // Form validation uses Blazilla (FluentValidation for EditForm). Components pass an explicit
    // validator instance (e.g. <FluentValidator Validator="new RoleDetailsValidator()"/>), so no
    // DI registration is required here. (Replaced the deprecated Blazored / unwired Morris path.)

    serviceCollection.AddScoped<ChatHubConnection>();
    serviceCollection.AddScoped<PasskeyCeremonyClient>();

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
