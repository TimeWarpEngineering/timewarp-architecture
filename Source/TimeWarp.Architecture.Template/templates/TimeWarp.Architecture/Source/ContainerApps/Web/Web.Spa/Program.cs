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

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ConfigureSettings(aServiceCollection, aConfiguration);
    aServiceCollection.AddBlazorState
    (
      (aOptions) =>
      {
        #if DEBUG
        aOptions.UseReduxDevTools(options => options.Trace = false);
        #endif
        aOptions.Assemblies =
          new[]
          {
              typeof(Web.Spa.AssemblyMarker).GetTypeInfo().Assembly,
              typeof(TimeWarp.State.Plus.AssemblyMarker).GetTypeInfo().Assembly,
          };
      }
    );

    aServiceCollection.AddFormValidation
    (
        aValidationConfiguration =>
        {
          aValidationConfiguration.AddFluentValidation(typeof(Web.Spa.AssemblyMarker).Assembly);
          ServiceDescriptor serviceDescriptor =
            aServiceCollection.First
            (
              aServiceDescriptor =>
                aServiceDescriptor.ServiceType.Name == nameof(ServiceCollectionOptionsValidator.ServiceValidator) &&
                aServiceDescriptor.Lifetime == ServiceLifetime.Scoped
            );

          aServiceCollection.Remove(serviceDescriptor);
        }
    );

    aServiceCollection.AddScoped<ChatHubConnection>();
    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ActiveActionBehavior<,>));
    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
    aServiceCollection.AddScoped<ApiService>();
    // Set the JSON serializer options
    aServiceCollection.Configure<JsonSerializerOptions>
    (
      aJsonSerializerOptions =>
      {
        //aJsonSerializerOptions.PropertyNameCaseInsensitive = false;
        aJsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        ;//aJsonSerializerOptions.WriteIndented = true;
      }
    );

#if grpc
    SuperheroModule.ConfigureServices(aServiceCollection, aConfiguration);
#endif
    aServiceCollection.AddSingleton(aServiceCollection);
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<ServiceCollectionOptions, ServiceCollectionOptionsValidator>(aConfiguration);

    //aServiceCollection.ValidateOptions();
  }
}
