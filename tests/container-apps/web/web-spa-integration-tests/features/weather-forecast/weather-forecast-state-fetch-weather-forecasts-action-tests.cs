#region Purpose
// WeatherForecastsState.FetchWeatherForecasts against the closed-box API (quarantined — task 058).
// No SetupOnce host while the only fact is [Skip]: avoids a full Aspire boot for zero work
// (review 145-006 R1-2). Re-add SpaIntegrationHost lifecycle when un-quarantining.
#endregion

namespace WeatherForecastsState_;

[TestTag("Integration")]
public class FetchWeatherForecasts_Action_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchWeatherForecasts_Action_Should>();

  [Skip("Quarantined (task 058): SPA→server weather fetch not yet proven green under the headless " +
        "AspireSpaTestApplication host. Re-add SpaIntegrationHost SetupOnce/CleanUpOnce when un-skipping.")]
  public static Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
  {
    // Placeholder until 058 wires the fetch path; body intentionally empty under Skip.
    return Task.CompletedTask;
  }
}
