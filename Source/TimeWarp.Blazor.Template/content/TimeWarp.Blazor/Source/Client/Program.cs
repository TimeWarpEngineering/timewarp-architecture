namespace TimeWarp.Blazor.Client
{
  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Microsoft.Extensions.DependencyInjection;
  using BlazorState;
  using System.Reflection;
  using MediatR;
  using TimeWarp.Blazor.Features.ClientLoaders.Client;
  using TimeWarp.Blazor.Features.EventStreams.Client;
  using TimeWarp.Blazor.Components;

  public class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);
      builder.RootComponents.Add<App>("app");
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
      aServiceCollection.AddBaseAddressHttpClient();
    }
  }
}
