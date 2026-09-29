#region Purpose
// Debug/test surface for WeatherForecastsState: DevTools hydration and a test-only seeder.
#endregion

#region Design
// Hydrate rebuilds state from a Redux DevTools JSON snapshot to support time-travel debugging;
// the camelCase serializer options must match how DevTools serialized the state out.
// Internal Initialize(list) lets integration tests seed forecasts without hitting the API.
// TimeWarp.State's ThrowIfNotTestAssembly blocks production callers (case-insensitive since
// 12.0.0-beta.5, so the kebab test assembly passes — timewarp-state#607).
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

partial class WeatherForecastsState
{
  private static readonly JsonSerializerOptions JsonSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public override WeatherForecastsState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    string json = keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(WeatherForecasts))].ToString() ?? throw new InvalidOperationException();

    WeatherForecastsState newWeatherForecastsState = new()
    {
      WeatherForecastList = JsonSerializer.Deserialize<List<TWeatherForecast>>(json, JsonSerializerOptions) ?? throw new InvalidOperationException(),
      Guid = new Guid(keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
    };

    return newWeatherForecastsState;
  }

  internal void Initialize(List<TWeatherForecast> weatherForecastList)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    WeatherForecastList = Guard.Against.Null(weatherForecastList);
  }
}
