namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.Spa.WeatherForecastsState;

public class FetchWeatherForecastsAction_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public FetchWeatherForecastsAction_Should
  (
    SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

    await Send(fetchWeatherForecastsRequest);

    WeatherForecastsState.WeatherForecasts.Count.Should().BeGreaterThan(0);
  }
}
