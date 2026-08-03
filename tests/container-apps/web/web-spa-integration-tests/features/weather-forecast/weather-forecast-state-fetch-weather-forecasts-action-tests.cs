#region Purpose
// WeatherForecastsState.FetchWeatherForecasts against the closed-box API (quarantined — task 058).
// No SetupOnce host while the only fact is [Skip]: avoids the shared Aspire boot for zero work
// (review 145-006 R1-2; still holds under 145-008 session sharing — no SetupOnce means this
// class never calls SessionFixture.GetAsync, so it never triggers SpaSessionFixture's create).
// Re-add SetupOnce (Session = await SessionFixture.GetAsync<SpaSessionFixture>()) when
// un-quarantining — see the other feature classes in this suite for the shape.
#endregion

namespace WeatherForecastsState_;

[TestTag("Integration")]
public class FetchWeatherForecasts_Action_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchWeatherForecasts_Action_Should>();

  [Skip("Quarantined (task 058): SPA→server weather fetch not yet proven green under the headless " +
        "AspireSpaTestApplication host. Re-add SetupOnce (SessionFixture.GetAsync<SpaSessionFixture>) " +
        "when un-skipping.")]
  public static Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    // Placeholder until 058 wires the fetch path; body intentionally empty under Skip.
    return Task.CompletedTask;
  }
}
