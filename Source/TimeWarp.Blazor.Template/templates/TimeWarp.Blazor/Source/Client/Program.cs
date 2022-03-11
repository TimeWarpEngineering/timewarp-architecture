namespace TimeWarp.Blazor.Client;

using BlazorState;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using MediatR;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PeterLeslieMorris.Blazor.Validation;
using ProtoBuf.Grpc.Client;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using TimeWarp.Blazor.Analyzer;
using TimeWarp.Blazor.Components;
using TimeWarp.Blazor.Configuration;
using TimeWarp.Blazor.Features.Applications;
using TimeWarp.Blazor.Features.ClientLoaders;
using TimeWarp.Blazor.Features.EventStreams;
using TimeWarp.Blazor.Features.Superheros;
using ServiceCollection = Configuration.ServiceCollection;

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
        aOptions.UseReduxDevToolsBehavior = true;
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
      aValidationConfiguration => aValidationConfiguration.AddFluentValidation(typeof(Program).Assembly)
    );

    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ProcessingBehavior<,>));
    aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
    aServiceCollection.AddScoped<ClientLoader>();
    aServiceCollection.AddScoped<IClientLoaderConfiguration, ClientLoaderConfiguration>();
    aServiceCollection.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    aServiceCollection.AddScoped<WebApiService>();

    ConfigureGrpc(aServiceCollection);

#if DEBUG
    new ProjectAnlayzer().Analyze();
#endif
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<ServiceCollection, ServiceCollectionValidator>(aConfiguration);

    aServiceCollection.ValidateOptions();
  }

  private static void ConfigureGrpc(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddSingleton
    (
      aServiceProvider =>
      {
        IConfiguration configuration = aServiceProvider.GetRequiredService<IConfiguration>();
        const string serviceName = "timewarp-blazor-server";
        string backendUrl = GetServiceUri(configuration, serviceName);

        // If no address is set then fallback to the current webpage URL
        if (string.IsNullOrEmpty(backendUrl))
        {
          //NavigationManager navigationManager = aServiceProvider.GetRequiredService<NavigationManager>();
          backendUrl = "https://localhost:5001";
        }

        Console.WriteLine($"backendUrl:{backendUrl}");

        // Create a channel with a GrpcWebHandler that is addressed to the backend server.
        //
        // GrpcWebText is used because server streaming requires it. If server streaming is not used in your app
        // then GrpcWeb is recommended because it produces smaller messages.
        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

        return GrpcChannel.ForAddress
        (
          backendUrl,
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

  private static string GetServiceUri(IConfiguration aConfiguration, string aServiceName)
  {
    var uriBuilder = new UriBuilder
    {
      Scheme = aConfiguration.GetValue<string>($"service:{aServiceName}:protocol"),
      Host = aConfiguration.GetValue<string>($"service:{aServiceName}:host"),
      Port = aConfiguration.GetValue<int>($"service:{aServiceName}:port")
    };

    return aConfiguration.GetServiceUri(aServiceName)?.AbsoluteUri ?? uriBuilder.ToString();
  }

  public static Task Main(string[] aArgumentArray)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(aArgumentArray);
    builder.RootComponents.Add<App>("#app");
    builder.Services.AddScoped
      (_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

    ConfigureServices(builder.Services, builder.Configuration);

    WebAssemblyHost host = builder.Build();
    return host.RunAsync();
  }
}
