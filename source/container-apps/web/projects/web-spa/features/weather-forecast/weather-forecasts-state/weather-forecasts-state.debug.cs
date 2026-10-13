#region Purpose
// Test-only seeder for WeatherForecastsState.
#endregion

#region Design
// Internal Initialize(list) lets integration tests seed forecasts without hitting the API.
// TimeWarp.State's ThrowIfNotTestAssembly blocks production callers (open only after the test host calls StateTestOptions.Enable(), 12.0.0-beta.11).
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

partial class WeatherForecastsState
{
  internal void Initialize(List<TWeatherForecast> weatherForecastList)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    WeatherForecastList = Guard.Against.Null(weatherForecastList);
  }
}
