#region Purpose
// WeatherForecastsState.Clone identity and value semantics under the SPA test host.
#endregion

namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

[TestTag("Integration")]
public class Clone_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Clone_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    // Session-owned: the Jaribu session hook disposes SpaSessionFixture; do not dispose here.
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Clone()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    WeatherForecastsState weatherForecastsState = scope.Store.GetState<WeatherForecastsState>();

    var weatherForecasts = new List<TWeatherForecast>
    {
      new
      (
        date: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        summary: "Summary 1",
        temperatureC: 24
      ),
      new
      (
        date: new DateTime(2019, 5, 17, 0, 0, 0, DateTimeKind.Utc),
        summary: "Summary 2",
        temperatureC: 25
      )
    };
    weatherForecastsState.Initialize(weatherForecasts);

    WeatherForecastsState clone = weatherForecastsState.Clone();

    weatherForecastsState.ShouldNotBeSameAs(clone);
    weatherForecastsState.WeatherForecasts!.Count.ShouldBe(clone.WeatherForecasts!.Count);
    weatherForecastsState.Guid.ShouldNotBe(clone.Guid);
    weatherForecastsState.WeatherForecasts[0].TemperatureC.ShouldBe(clone.WeatherForecasts[0].TemperatureC);
    weatherForecastsState.WeatherForecasts[0].ShouldNotBeSameAs(clone.WeatherForecasts[0]);
    return Task.CompletedTask;
  }
}
