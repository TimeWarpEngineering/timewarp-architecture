#region Purpose
// Root partial for WeatherForecastsState: holds the forecast list fetched from the Api service.
#endregion

#region Design
// Private list with a read-only projection enforces that only action handlers mutate state.
// Null vs empty is meaningful: Initialize nulls the list so UI can distinguish "not loaded"
// (render a loading indicator) from "loaded with zero rows".
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
