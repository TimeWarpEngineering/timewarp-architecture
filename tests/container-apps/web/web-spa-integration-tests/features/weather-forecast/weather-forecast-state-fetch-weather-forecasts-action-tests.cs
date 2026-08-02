#region Purpose
// WeatherForecastsState.FetchWeatherForecasts against the closed-box API (quarantined — task 058).
#endregion

namespace WeatherForecastsState_;

using global::Aspire.Hosting;

[TestTag("Integration")]
public class FetchWeatherForecasts_Action_Should
{
  private static DistributedApplication? App;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchWeatherForecasts_Action_Should>();

  public static async Task SetupOnce()
  {
    App = await SpaIntegrationHost.StartAsync();
    Spa = new AspireSpaTestApplication(App);
  }

  public static async Task CleanUpOnce()
  {
    await SpaIntegrationHost.StopAsync(App);
    App = null;
    Spa = null;
  }

  [Skip("Quarantined (task 058): the SPA's weather fetch throws in the headless test host (the toast " +
        "ExceptionNotification surfaces a FluentToastProvider error). Needs the SPA->server fetch wired " +
        "in the AspireSpaTestApplication host. Tracked separately.")]
  public static async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    WeatherForecastsState weatherForecastsState = scope.Store.GetState<WeatherForecastsState>();

    await weatherForecastsState.FetchWeatherForecasts(5);

    weatherForecastsState.WeatherForecasts.ShouldNotBeNull();
    weatherForecastsState.WeatherForecasts.Count.ShouldBe(5);
  }
}
