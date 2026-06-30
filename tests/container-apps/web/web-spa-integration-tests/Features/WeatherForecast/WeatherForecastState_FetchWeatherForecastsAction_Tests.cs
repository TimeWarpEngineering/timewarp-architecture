namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

public class FetchWeatherForecasts_Action_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public FetchWeatherForecasts_Action_Should
  (
    ISpaTestApplication spaTestApplication
  ) : base(spaTestApplication) { }

  [Skip("Quarantined (task 058): the SPA's weather fetch throws in the headless test host (the toast " +
        "ExceptionNotification surfaces a FluentToastProvider error). Needs the SPA->server fetch wired " +
        "in the SpaTestApplication host. Tracked separately.")]
  public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    await WeatherForecastsState.FetchWeatherForecasts(5);

    WeatherForecastsState.WeatherForecasts.ShouldNotBeNull();
    WeatherForecastsState.WeatherForecasts.Count.ShouldBe(5);
  }
}
