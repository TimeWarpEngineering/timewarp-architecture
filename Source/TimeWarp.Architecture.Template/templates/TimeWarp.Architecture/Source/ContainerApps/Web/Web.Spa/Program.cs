namespace TimeWarp.Architecture.Web.Spa;

using BlazorState;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeterLeslieMorris.Blazor.Validation;
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
