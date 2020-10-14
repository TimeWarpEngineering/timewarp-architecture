namespace TimeWarp.Blazor.Client
{
  using BlazorState;
  using MediatR;
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Microsoft.Extensions.DependencyInjection;
  using PeterLeslieMorris.Blazor.Validation;
  using System;
  using System.Net.Http;
  using System.Reflection;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Analyzer;
  using TimeWarp.Blazor.Components;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using TimeWarp.Blazor.Features.EventStreams;

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

#if DEBUG
      new ProjectAnlayzer().Analyze();
#endif
    }

    public static Task Main(string[] aArgumentArray)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(aArgumentArray);
      builder.RootComponents.Add<App>("app");
      builder.Services.AddSingleton
      (
        new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
      );
      ConfigureServices(builder.Services);

      WebAssemblyHost host = builder.Build();
      return host.RunAsync();
    }
  }
}
