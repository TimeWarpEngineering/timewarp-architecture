namespace TimeWarp.Blazor.Client.Integration.Tests.Features.WeatherForecast
{
  using BlazorState;
  using MediatR;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Client.WeatherForecastFeature;
  using static TimeWarp.Blazor.Client.WeatherForecastFeature.WeatherForecastsState;

  internal class FetchWeatherForecastTests
  {
    private readonly IMediator Mediator;
    private readonly IServiceProvider ServiceProvider;
    private readonly IStore Store;
    private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

    public FetchWeatherForecastTests(TestFixture aTestFixture)
    {
      ServiceProvider = aTestFixture.ServiceProvider;
      Mediator = ServiceProvider.GetService<IMediator>();
      Store = ServiceProvider.GetService<IStore>();
    }

    public async Task Should_Fetch_WeatherForecasts()
    {
      var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

      _ = await Mediator.Send(fetchWeatherForecastsRequest);

      WeatherForecastsState.WeatherForecasts.Count.ShouldBeGreaterThan(0);
    }
  }
}