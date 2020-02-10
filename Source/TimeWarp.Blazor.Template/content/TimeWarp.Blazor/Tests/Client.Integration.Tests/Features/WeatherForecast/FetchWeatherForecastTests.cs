namespace TimeWarp.Blazor.Features.WeatherForecasts.Tests.Client
{
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecasts.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;
  using static TimeWarp.Blazor.Features.WeatherForecasts.Client.WeatherForecastsState;

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
