namespace TimeWarp.Blazor.Client.Integration.Tests.Features.WeatherForecast
{
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Client.WeatherForecastFeature;
  using static TimeWarp.Blazor.Client.WeatherForecastFeature.WeatherForecastsState;

  internal class FetchWeatherForecast2Tests : BaseTest
  {
    public FetchWeatherForecast2Tests(IWebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public async Task Should_Fetch_WeatherForecastsAsync()
    {
      var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

      await Send(fetchWeatherForecastsRequest);
      WeatherForecastsState weatherForecastsState = Store.GetState<WeatherForecastsState>();

      weatherForecastsState.WeatherForecasts.Count.ShouldBeGreaterThan(0);
    }
  }
}
