namespace TimeWarp.Architecture.Web.Spa;

using BlazorState;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeterLeslieMorris.Blazor.Validation;
using ProtoBuf.Grpc.Client;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using TimeWarp.Architecture.Analyzer;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.ClientLoaders;
using TimeWarp.Architecture.Features.EventStreams;
using TimeWarp.Architecture.Features.Superheros;
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
        aOptions.UseReduxDevTools( options => options.Trace = true);
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

    ConfigureGrpc(aServiceCollection);

#if DEBUG
    new ProjectAnlayzer().Analyze();
#endif
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<ServiceCollectionOptions, ServiceCollectionOptionsValidator>(aConfiguration);

    //aServiceCollection.ValidateOptions();
  }

  private static void ConfigureGrpc(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddSingleton
    (
      aServiceProvider =>
      {
        IConfiguration configuration = aServiceProvider.GetRequiredService<IConfiguration>();

        Uri grpcUrl = GetServiceUri(configuration, Constants.GrpcServiceName);

        Console.WriteLine($"grpcUrl:{grpcUrl}");

        if (grpcUrl is null)
        {
          throw new Exception($"No {Constants.GrpcServiceName} address found in configuration");
        }


        // Create a channel with a GrpcWebHandler that is addressed to the backend server.
        //
        // GrpcWebText is used because server streaming requires it. If server streaming is not used in your app
        // then GrpcWeb is recommended because it produces smaller messages.
        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

        return GrpcChannel.ForAddress
        (
          grpcUrl,
          new GrpcChannelOptions
          {
            HttpHandler = httpHandler,
            //CompressionProviders = ...,
            //Credentials = ...,
            //DisposeHttpClient = ...,
            //HttpClient = ...,
            //LoggerFactory = ...,
            //MaxReceiveMessageSize = ...,
            //MaxSendMessageSize = ...,
            //ThrowOperationCanceledOnCancellation = ...,
          }
        );
      }
    );

    aServiceCollection.AddSingleton<ISuperheroService>
    (
      aServiceProvider =>
      {
        GrpcChannel grpcChannel = aServiceProvider.GetRequiredService<GrpcChannel>();
        return grpcChannel.CreateGrpcService<ISuperheroService>();
      }
    );

  }

  private static Uri GetServiceUri(IConfiguration aConfiguration, string aServiceName)
  {
    ServiceCollectionOptions serviceCollectionOptions =
      aConfiguration.GetSection(nameof(ServiceCollectionOptions)).Get<ServiceCollectionOptions>();

    ServiceCollectionOptions.Service service = serviceCollectionOptions[Constants.GrpcServiceName];

    Console.WriteLine($"service.Host:{service.Host}");

    var uriBuilder = new UriBuilder
    {
      Scheme = service.Protocol,
      Host = service.Host,
      Port = service.Port
    };

    Uri serviceUri = aConfiguration.GetServiceUri(aServiceName) ?? uriBuilder.Uri;

    return serviceUri;
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
