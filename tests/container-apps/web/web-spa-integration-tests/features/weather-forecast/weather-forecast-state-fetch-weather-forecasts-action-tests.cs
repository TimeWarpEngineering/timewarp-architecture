#region Purpose
// WeatherForecastsState.FetchWeatherForecasts against the closed-box API through ingress.
#endregion

#region Design
// Session-shared Aspire host (SpaSessionFixture). The SPA test host points the api-server
// HttpClient at ingress and authenticates with MockAccessTokenProvider. Days=5 matches the
// sample handler's documented example and the five-forecast assertion.
#endregion

namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;
using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

[TestTag("Integration")]
public class FetchWeatherForecasts_Action_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchWeatherForecasts_Action_Should>();

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

  public static async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    await scope.Send(new FetchWeatherForecastsActionSet.Action(days: 5));

    IReadOnlyList<TWeatherForecast>? forecasts =
      scope.Store.GetState<WeatherForecastsState>().WeatherForecasts;
    TimeWarp.Architecture.Features.NotificationState toast =
      scope.Store.GetState<TimeWarp.Architecture.Features.NotificationState>();
    string bars = string.Join(" | ", toast.Messages.Select(message => $"{message.Intent}:{message.Title}"));
    forecasts.ShouldNotBeNull(bars);
    forecasts.Count.ShouldBe(5);
  }
}
