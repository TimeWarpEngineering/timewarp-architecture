#region Purpose
// Blazor WebAssembly entry point: composes auth, TimeWarp.State, and API services for the SPA.
#endregion

#region Design
// ConfigureServices is public static so integration tests build the same container as the app.
// MOCK_AUTHENTICATION (all configs — task 131 F-009) and optional MOCK_WEB_API swap in offline
// fakes so Debug and Release agree; the non-mock compile path is MSAL/AzureAdB2C until 104-021
// makes Entra non-default explicitly. Template symbols (api, grpc) trim optional services.
// API services are registered via explicit factories because they expose extra constructors for
// testing and DI must not guess which one to use.
// Default culture is forced to ISO date patterns so date rendering/parsing is deterministic
// regardless of the browser locale.
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

    ConfigureServices(builder.Services, builder.Configuration);
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

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {

//-:cnd:noEmit
#if MOCK_AUTHENTICATION
    serviceCollection.AddScoped<AuthenticationStateProvider, MockAuthenticationStateProvider>();
    serviceCollection.AddScoped<IAccessTokenProvider, MockAccessTokenProvider>();
#else
    serviceCollection.AddMsalAuthentication
    (
      options =>
      {
        configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
        options.ProviderOptions.LoginMode = "Redirect";
      }
    ).AddAccountClaimsPrincipalFactory<AccountClaimsPrincipalFactoryWithRoles>();
#endif
//+:cnd:noEmit

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
