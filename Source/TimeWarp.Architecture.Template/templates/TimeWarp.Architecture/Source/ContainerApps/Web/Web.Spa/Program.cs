namespace TimeWarp.Architecture.Web.Spa;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Chat.Contracts;
using ServiceCollectionOptions = Configuration.ServiceCollectionOptions;

public class Program
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ConfigureSettings(aServiceCollection, aConfiguration);
    aServiceCollection.AddBlazorState
    (
      (aOptions) =>
      {
#if ReduxDevToolsEnabled
        aOptions.UseReduxDevTools(options => options.Trace = false);
#endif
        aOptions.Assemblies =
          new Assembly[]
          {
              typeof(Program).GetTypeInfo().Assembly,
          };
      }
    );

    aServiceCollection.AddFormValidation
    (
        aValidationConfiguration =>
        {
          aValidationConfiguration.AddFluentValidation(typeof(Web_Spa_Assembly).Assembly);
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

    aServiceCollection.AddScoped<ChatHubConnection>
    (
      serviceProvider =>
      {
        var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
        var chatHubUrl = new Uri(new Uri(navigationManager.BaseUri), ChatHubConstants.Route);
        return new ChatHubConnection(chatHubUrl.ToString());
      }
    );

    //aServiceCollection.AddScoped((Func<IServiceProvider, ChatHubConnection>)(serviceProvider =>
    //{
    //  NavigationManager navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
    //  string chatHubUrl = $"{navigationManager.BaseUri}{Features.Chat.Contracts.ChatHubConstants.Route}";
    //  return new ChatHubConnection(chatHubUrl);
    //}));


    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ProcessingBehavior<,>));
    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
    aServiceCollection.AddScoped<ClientLoader>();
    aServiceCollection.AddScoped<IClientLoaderConfiguration, ClientLoaderConfiguration>();
    aServiceCollection.AddScoped<WebApiService>();
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

  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
    builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

    ConfigureServices(builder.Services, builder.Configuration);

    await builder.Build().RunAsync();
  }
}
