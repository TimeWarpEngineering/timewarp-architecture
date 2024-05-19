namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

public class FetchWeatherForecasts_Action_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public FetchWeatherForecasts_Action_Should
  (
    SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    var fetchWeatherForecastsRequest = new FetchWeatherForecasts.Action(5);

    await Send(fetchWeatherForecastsRequest);

    WeatherForecastsState.WeatherForecasts.Count.Should().Be(5);
  }
}
