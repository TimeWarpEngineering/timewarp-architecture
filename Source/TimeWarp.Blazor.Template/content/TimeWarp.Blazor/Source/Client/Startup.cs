namespace TimeWarp.Blazor.Client
{
  using BlazorState;
  using MediatR;
  using Microsoft.AspNetCore.Components.Builder;
  using Microsoft.Extensions.DependencyInjection;
  using System.Reflection;
  using TimeWarp.Blazor.Client.ApplicationFeature;
  using TimeWarp.Blazor.Client.ClientLoaderFeature;
  using TimeWarp.Blazor.Client.CounterFeature;
  using TimeWarp.Blazor.Client.EventStreamFeature;
  using TimeWarp.Blazor.Client.WeatherForecastFeature;

  public class Startup
  {
    public void Configure(IComponentsApplicationBuilder aComponentsApplicationBuilder) =>
      aComponentsApplicationBuilder.AddComponent<App>("app");

    public void ConfigureServices(IServiceCollection aServiceCollection)
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
                typeof(Startup).GetTypeInfo().Assembly,
            };
        }
      );

      aServiceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventStreamBehavior<,>));
      aServiceCollection.AddScoped<ClientLoader>();
      aServiceCollection.AddScoped<IClientLoaderConfiguration, ClientLoaderConfiguration>();

      aServiceCollection.AddTransient<ApplicationState>();
      aServiceCollection.AddTransient<CounterState>();
      aServiceCollection.AddTransient<EventStreamState>();
      aServiceCollection.AddTransient<WeatherForecastsState>();
    }
  }
}
