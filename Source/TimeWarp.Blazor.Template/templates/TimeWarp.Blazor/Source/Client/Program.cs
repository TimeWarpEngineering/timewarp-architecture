namespace TimeWarp.Blazor.Client
{
  using BlazorState;
  using MediatR;
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Microsoft.Extensions.DependencyInjection;
  using System.Net.Http;
  using System.Reflection;
  using System.Threading.Tasks;
  using System;
  using TimeWarp.Blazor.Components;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using TimeWarp.Blazor.Features.EventStreams;

  public class Program
  {
    public static async Task Main(string[] aArgumentArray)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(aArgumentArray);
      builder.RootComponents.Add<App>("app");
      builder.Services.AddSingleton
      (
        new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
      );
      ConfigureServices(builder.Services);

      WebAssemblyHost host = builder.Build();
      await host.RunAsync();
    }

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

      aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
      aServiceCollection.AddScoped<ClientLoader>();
      aServiceCollection.AddScoped<IClientLoaderConfiguration, ClientLoaderConfiguration>();
    }
  }
}