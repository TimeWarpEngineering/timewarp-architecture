namespace TimeWarp.Architecture.Web.Spa;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    ConfigureServices(builder.Services, builder.Configuration);
    builder.Services.AddHttpClient(Constants.ApiServiceName, aClient => aClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

    await builder.Build().RunAsync();
  }

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddBlazoredSessionStorage();
    serviceCollection.AddBlazoredLocalStorage();

    ConfigureSettings(serviceCollection, configuration);
    serviceCollection.AddBlazorState
    (
      (blazorStateOptions) =>
      {
        #if DEBUG
        blazorStateOptions.UseReduxDevTools(reduxDevToolsOptions => reduxDevToolsOptions.Trace = false);
        #endif

        blazorStateOptions.Assemblies =
          new[]
          {
            // ReSharper disable once RedundantNameQualifier
            typeof(Web.Spa.AssemblyMarker).GetTypeInfo().Assembly, typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );

    serviceCollection.AddFormValidation
    (
      aValidationConfiguration =>
      {
        aValidationConfiguration.AddFluentValidation(typeof(Web.Spa.AssemblyMarker).Assembly);
        ServiceDescriptor serviceDescriptor =
          serviceCollection.First
          (
            aServiceDescriptor =>
              aServiceDescriptor.ServiceType.Name == nameof(ServiceCollectionOptionsValidator.ServiceValidator) &&
              aServiceDescriptor.Lifetime == ServiceLifetime.Scoped
          );

        serviceCollection.Remove(serviceDescriptor);
      }
    );

    serviceCollection.AddScoped<ChatHubConnection>();
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ActiveActionBehavior<,>));
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));

    serviceCollection.AddScoped<ApiService>();
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
      .ConfigureOptions<ServiceCollectionOptions, ServiceCollectionOptionsValidator>(aConfiguration)
      .ConfigureOptions<BlazorSettings, BlazorSettingsValidator>(aConfiguration);

    //aServiceCollection.ValidateOptions();
  }
}
