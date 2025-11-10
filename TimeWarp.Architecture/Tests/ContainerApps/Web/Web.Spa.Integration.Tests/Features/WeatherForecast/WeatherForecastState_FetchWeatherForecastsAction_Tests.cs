namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

public class FetchWeatherForecasts_Action_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public FetchWeatherForecasts_Action_Should
  (
    ISpaTestApplication aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    await WeatherForecastsState.FetchWeatherForecasts(5);

    WeatherForecastsState.WeatherForecasts.ShouldNotBeNull();
    WeatherForecastsState.WeatherForecasts.Count.ShouldBe(5);
  }
}
