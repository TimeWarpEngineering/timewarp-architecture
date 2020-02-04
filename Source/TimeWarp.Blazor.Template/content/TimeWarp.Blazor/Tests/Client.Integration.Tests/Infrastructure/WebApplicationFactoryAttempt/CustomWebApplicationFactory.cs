namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using BlazorState;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;
  using System.Net.Http;
  using System.Reflection;
  using System.Text.Json;
  using TimeWarp.Blazor.Client.ApplicationFeature;
  using TimeWarp.Blazor.Client.ClientLoaderFeature;
  using TimeWarp.Blazor.Client.CounterFeature;
  using TimeWarp.Blazor.Client.EventStreamFeature;
  using TimeWarp.Blazor.Client.WeatherForecastFeature;

  public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
  {

    private readonly HttpClient HttpClient;

    public CustomWebApplicationFactory(HttpClient aHttpClient)
    {
      HttpClient = aHttpClient;
    }

    //protected override IWebHostBuilder CreateWebHostBuilder()
    //{
    //  IWebAssemblyHostBuilder webAssemblyHostBuilder = BlazorWebAssemblyHost.CreateDefaultBuilder()
    //    .ConfigureServices(ConfigureServices);

    //  return webAssemblyHostBuilder;
    //}

    protected override IHost CreateHost(IHostBuilder builder)
    {
      IWebAssemblyHostBuilder webAssemblyHostBuilder = BlazorWebAssemblyHost.CreateDefaultBuilder()
        .ConfigureServices(ConfigureServices);

      return webAssemblyHostBuilder as IHost;
    }

    //protected override void ConfigureWebHost(IWebHostBuilder aWebHostBuilder)
    //{
    //  base.ConfigureWebHost(aWebHostBuilder);
    //  aWebHostBuilder.ConfigureServices
    //  (
    //    aServiceCollection =>
    //    {
    //      //aServiceCollection.Clear();
    //      //HttpClient httpClient = CreateClient();
    //      //aServiceCollection.AddSingleton(httpClient);
    //      //ConfigureServices(aServiceCollection);
    //    }
    //  );
    //}

    //public void Configure
    //(
    //  IApplicationBuilder aApplicationBuilder,
    //  IWebHostEnvironment aWebHostEnvironment
    //)
    //{
    //  aApplicationBuilder.UseResponseCompression();

    //  if (aWebHostEnvironment.IsDevelopment())
    //  {
    //    aApplicationBuilder.UseDeveloperExceptionPage();
    //    aApplicationBuilder.UseBlazorDebugging();
    //  }

    //  aApplicationBuilder.UseRouting();
    //  aApplicationBuilder.UseEndpoints
    //  (
    //    aEndpointRouteBuilder =>
    //    {
    //      aEndpointRouteBuilder.MapControllers(); // We use explicit attribute routing so dont need MapDefaultControllerRoute
    //      aEndpointRouteBuilder.MapBlazorHub();
    //      aEndpointRouteBuilder.MapFallbackToPage("/_Host");
    //    }
    //  );
    //  aApplicationBuilder.UseStaticFiles();
    //  aApplicationBuilder.UseClientSideBlazorFiles<Client.Startup>();
    //}

    private void ConfigureServices(IServiceCollection aServiceCollection)
    {
      // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
      aServiceCollection.AddSingleton(HttpClient);
      aServiceCollection.AddBlazorState
      (
        aOptions => aOptions.Assemblies =
        new Assembly[] { typeof(Startup).GetTypeInfo().Assembly }
      );

      aServiceCollection.AddSingleton
      (
        new JsonSerializerOptions
        {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }
      );

      aServiceCollection.AddSingleton<IClientLoaderConfiguration, ClientLoaderTestConfiguration>();
      aServiceCollection.AddTransient<ApplicationState>();
      aServiceCollection.AddTransient<CounterState>();
      aServiceCollection.AddTransient<EventStreamState>();
      aServiceCollection.AddTransient<WeatherForecastsState>();
    }
  }
}
