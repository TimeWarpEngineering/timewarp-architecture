#region Purpose
// Blazor WebAssembly entry point: composes auth, TimeWarp.State, and API services for the SPA.
#endregion

#region Design
// ConfigureServices is public static so integration tests and Web.Server prerender compose the
// same container as the app. Auth is runtime-config-gated (tasks 145-009 + 104-021):
//   1. Development/Testing + Authentication:UseMock → MockAuthenticationRegistration
//   2. else Authentication:UseEntra → MSAL / AzureAdB2C (opt-in enterprise path)
//   3. else IdentitySessionAuthenticationRegistration (passkey cookie via GetCurrentSession)
// Fail-closed: Production never activates mock even when UseMock is true. UseEntra defaults false
// so the non-mock happy path does not require Entra. optional MOCK_WEB_API still compile-time for
// offline SPA API fakes. Template symbols (api, grpc) trim optional services. API services use
// explicit factories so DI does not guess constructors. Default culture is forced to ISO date
// patterns for deterministic rendering.
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
//
// No new template.json feature flag for identity/x402 (they ship with the template); Entra is a
// runtime config switch, not a compile-time DefineConstants symbol (avoids TWA0008/0010 dual paths).
#endregion

namespace TimeWarp.Architecture.Web.Spa;

using System.Globalization;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    SetIsoCulture();
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    ConfigureServices(builder.Services, builder.Configuration, builder.HostEnvironment.Environment);
    builder.Services.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
#if api
    builder.Services.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
#endif

    await builder.Build().RunAsync();
  }

  private static void SetIsoCulture()
  {
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
      if (MockAuthenticationDefaults.IsEntraAuthActive(configuration[MockAuthenticationDefaults.UseEntraKey]))
      {
        serviceCollection.AddMsalAuthentication
        (
          options =>
          {
            configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
            options.ProviderOptions.LoginMode = "Redirect";
          }
        ).AddAccountClaimsPrincipalFactory<AccountClaimsPrincipalFactoryWithRoles>();
      }
      else
      {
        // Default non-mock path: first-party passkey identity-session (no Entra).
        IdentitySessionAuthenticationRegistration.AddSpaIdentitySessionAuthentication(serviceCollection);
      }
    }

    // Add authorization services
    serviceCollection.AddAuthorizationCore(PolicyRegistration.AddPolicies);
    // Register the custom requirements handlers
    serviceCollection.AddFluentUIComponents();
    serviceCollection.AddBlazoredSessionStorage();
    serviceCollection.AddBlazoredLocalStorage();

    ConfigureSettings(serviceCollection, configuration);
    serviceCollection.AddTimeWarpState
    (
      timeWarpStateOptions =>
      {
        //-:cnd:noEmit
#if ReduxDevToolsEnabled
        timeWarpStateOptions.UseReduxDevTools(reduxDevToolsOptions => reduxDevToolsOptions.Trace = false);
#endif
        //+:cnd:noEmit

        timeWarpStateOptions.Assemblies =
          new[]
          {
            // ReSharper disable once RedundantNameQualifier
            typeof(Web.Spa.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );

    // Form validation uses Blazilla (FluentValidation for EditForm). Components pass an explicit
    // validator instance (e.g. <FluentValidator Validator="new RoleDetailsValidator()"/>), so no
    // DI registration is required here. (Replaced the deprecated Blazored / unwired Morris path.)

    serviceCollection.AddScoped<ChatHubConnection>();
    serviceCollection.AddScoped<PasskeyCeremonyClient>();
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ActiveActionBehavior<,>));
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));

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
