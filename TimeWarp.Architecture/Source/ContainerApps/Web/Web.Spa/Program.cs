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
    builder.Services.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

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

    // Add authorization services
    serviceCollection.AddAuthorizationCore(PolicyRegistration.AddPolicies);
    // Register the custom requirements handlers
    serviceCollection.AddFluentUIComponents();
    serviceCollection.AddBlazoredSessionStorage();
    serviceCollection.AddBlazoredLocalStorage();

    ConfigureSettings(serviceCollection, configuration);
    serviceCollection.AddBlazorState
    (
      blazorStateOptions =>
      {
        //-:cnd:noEmit
#if DEBUG
        blazorStateOptions.UseReduxDevTools(reduxDevToolsOptions => reduxDevToolsOptions.Trace = false);
#endif
        //+:cnd:noEmit

        blazorStateOptions.Assemblies =
          new[]
          {
            // ReSharper disable once RedundantNameQualifier
            typeof(Web.Spa.AssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );

    // TODO: Either Fix Petes or remove this and continue to use Blazored
    // serviceCollection.AddFormValidation
    // (
    //   aValidationConfiguration =>
    //   {
    //     aValidationConfiguration.AddFluentValidation(typeof(Web.Spa.AssemblyMarker).Assembly);
    //     ServiceDescriptor serviceDescriptor =
    //       serviceCollection.First
    //       (
    //         aServiceDescriptor =>
    //           aServiceDescriptor.ServiceType.Name == nameof(ServiceCollectionOptionsValidator.ServiceValidator) &&
    //           aServiceDescriptor.Lifetime == ServiceLifetime.Scoped
    //       );
    //
    //     serviceCollection.Remove(serviceDescriptor);
    //   }
    // );

    serviceCollection.AddScoped<ChatHubConnection>();
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ActiveActionBehavior<,>));
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));

    // TODO: Look into naming of the services. I think we can clear up the naming.
    serviceCollection.AddScoped<IWebServerApiService>
    (
      serviceProvider =>
      {
        IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        IOptions<JsonSerializerOptions> options = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();
        return new WebServerApiService(accessTokenProvider, httpClientFactory, options);
      }
    );

    serviceCollection.AddScoped<IApiServerApiService>
    (
      serviceProvider =>
      {
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        IOptions<JsonSerializerOptions> options = serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();
        return new ApiServerApiService(httpClientFactory, options);
      }
    );

    // Set the JSON serializer options
    serviceCollection.Configure<JsonSerializerOptions>
    (
      aJsonSerializerOptions =>
      {
        //aJsonSerializerOptions.PropertyNameCaseInsensitive = false;
        aJsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        ;//aJsonSerializerOptions.WriteIndented = true;
      }
    );

#if grpc
    SuperheroModule.ConfigureServices(serviceCollection, configuration);
#endif
    serviceCollection.AddSingleton(serviceCollection);
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      // .ConfigureOptions<ServiceCollectionOptions, ServiceCollectionOptionsValidator>(aConfiguration)
      .ConfigureOptions<BlazorSettings, BlazorSettingsValidator>(aConfiguration);

    //aServiceCollection.ValidateOptions();
  }
}
