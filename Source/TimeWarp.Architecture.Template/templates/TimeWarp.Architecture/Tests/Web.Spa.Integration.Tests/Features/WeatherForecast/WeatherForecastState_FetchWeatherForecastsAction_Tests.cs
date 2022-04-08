namespace WeatherForecastsState;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

public class FetchWeatherForecastsAction_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public FetchWeatherForecastsAction_Should
  (
    SpaTestApplication<YarpServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

    await Send(fetchWeatherForecastsRequest);

    WeatherForecastsState.WeatherForecasts.Count.Should().BeGreaterThan(0);
  }
}
