namespace TimeWarp.Blazor.Client
{
  using BlazorState;
  using Grpc.Net.Client;
  using Grpc.Net.Client.Web;
  using MediatR;
  using Microsoft.AspNetCore.Components;
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using PeterLeslieMorris.Blazor.Validation;
  using ProtoBuf.Grpc.Client;
  using System;
  using System.Net.Http;
  using System.Reflection;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Analyzer;
  using TimeWarp.Blazor.Components;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using TimeWarp.Blazor.Features.EventStreams;
  using TimeWarp.Blazor.Features.Hellos;
  using TimeWarp.Blazor.Features.Superheros;

  public class Program
  {
    public static void ConfigureServices(IServiceCollection aServiceCollection)
    {
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

      aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
      aServiceCollection.AddScoped<ClientLoader>();
      aServiceCollection.AddScoped<IClientLoaderConfiguration, ClientLoaderConfiguration>();

      ConfigureGrpc(aServiceCollection);

#if DEBUG
      new ProjectAnlayzer().Analyze();
#endif
    }

    private static void ConfigureGrpc(IServiceCollection aServiceCollection)
    {
      aServiceCollection.AddSingleton
      (
        aServiceProvider =>
        {
          // Get the service address from appsettings.json 
          IConfiguration config = aServiceProvider.GetRequiredService<IConfiguration>();
          string backendUrl = config["BackendUrl"];

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

    public static Task Main(string[] aArgumentArray)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(aArgumentArray);
      builder.RootComponents.Add<App>("#app");
      builder.Services.AddScoped
        (_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

      ConfigureServices(builder.Services);

      WebAssemblyHost host = builder.Build();
      return host.RunAsync();
    }
  }
}
