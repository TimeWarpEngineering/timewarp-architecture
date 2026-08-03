#region Purpose
// Blazor WebAssembly entry point: composes auth, TimeWarp.State, and API services for the SPA.
#endregion

#region Design
// ConfigureServices is public static so integration tests and Web.Server prerender compose the
// same container as the app. Auth is runtime-config-gated (task 145-009): Development/Testing +
// Authentication:UseMock → MockAuthenticationRegistration; otherwise MSAL/AzureAdB2C (104-021
// keeps Entra non-default). Fail-closed: Production never activates mock even when the flag is
// true. optional MOCK_WEB_API still compile-time for offline SPA API fakes. Template symbols
// (api, grpc) trim optional services. API services use explicit factories so DI does not guess
// constructors. Default culture is forced to ISO date patterns for deterministic rendering.
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
  /// gate (Development/Testing + Authentication:UseMock). When omitted (legacy 2-arg callers),
  /// reads ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT from configuration, defaulting to
  /// Production (no mock).
  /// </summary>
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration) =>
    ConfigureServices
    (
      serviceCollection,
      configuration,
      configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"]
    );

  public static void ConfigureServices
  (
    IServiceCollection serviceCollection,
    IConfiguration configuration,
    string? environmentName
  )
  {
    if (!MockAuthenticationRegistration.TryAddSpaMockAuthentication(serviceCollection, configuration, environmentName))
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
    serviceCollection.AddScoped<PasswordlessService>();
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

    serviceCollection
      .AddFluentValidatedOptions<PasswordlessOptions, PasswordlessOptionsValidator>(configuration)
      .ValidateOnStart();
  }
}
