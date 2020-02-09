namespace TimeWarp.Blazor.Integration.Tests.Features.WeatherForecast
{
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.WeatherForecastFeature;
  using static TimeWarp.Blazor.WeatherForecastFeature.WeatherForecastsState;

  internal class FetchWeatherForecastTests : BaseTest
  {
    private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

    public FetchWeatherForecastTests(WebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public async Task Should_Fetch_WeatherForecasts()
    {
      var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

      await Send(fetchWeatherForecastsRequest);

      WeatherForecastsState.WeatherForecasts.Count.ShouldBeGreaterThan(0);
    }
  }
}
