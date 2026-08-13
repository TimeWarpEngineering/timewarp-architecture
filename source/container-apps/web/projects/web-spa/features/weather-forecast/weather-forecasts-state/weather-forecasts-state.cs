#region Purpose
// Root partial for WeatherForecastsState: holds the forecast list fetched from the Api service.
#endregion

#region Design
// Private list with a read-only projection enforces that only action handlers mutate state.
// Null vs empty: no snapshot vs loaded with zero rows. In-flight fetch is [TrackAction]
// on FetchWeatherForecasts — the page uses IsAnyActive, not null.
// TWeatherForecast is the GetWeatherForecasts wire DTO (via using static) — the state stores
// the contract type directly instead of a mapped model to keep the template lean.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

[StateAccess]
public sealed partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<TWeatherForecast>? WeatherForecastList { get; set; } = [];

  public IReadOnlyList<TWeatherForecast>? WeatherForecasts => WeatherForecastList?.AsReadOnly();

  public override void Initialize() { WeatherForecastList = null; }
}
