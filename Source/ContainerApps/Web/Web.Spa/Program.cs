namespace TimeWarp.Architecture.Web.Spa;

using BlazorState;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Reflection;
using TimeWarp.Architecture.Components;


public class Program
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection.AddBlazorState
    (
      (aOptions) =>
      {
        aOptions.Assemblies =
          new Assembly[]
          {
            typeof(Program).GetTypeInfo().Assembly,
          };
      }
    );
  }

  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
    builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

    ConfigureServices(builder.Services, builder.Configuration);

    await builder.Build().RunAsync();
  }
}
