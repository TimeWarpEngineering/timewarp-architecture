#region Purpose
// Debug/test surface for WeatherForecastsState: DevTools hydration and a test-only seeder.
#endregion

#region Design
// Hydrate rebuilds state from a Redux DevTools JSON snapshot to support time-travel debugging;
// the camelCase serializer options must match how DevTools serialized the state out.
// Internal Initialize(list) lets integration tests seed forecasts without hitting the API.
// TestCaller.Ensure blocks production callers. TimeWarp.State's ThrowIfNotTestAssembly is
// case-sensitive on "Test" and rejects the kebab test assembly (timewarp-state#607).
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
    string json = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(WeatherForecasts))].ToString() ?? throw new InvalidOperationException();

    WeatherForecastsState newWeatherForecastsState = new()
    {
      WeatherForecastList = JsonSerializer.Deserialize<List<TWeatherForecast>>(json, JsonSerializerOptions) ?? throw new InvalidOperationException(),
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
    };

    return newWeatherForecastsState;
  }

  internal void Initialize(List<TWeatherForecast> weatherForecastList)
  {
    TimeWarp.Architecture.TestCaller.Ensure(Assembly.GetCallingAssembly());
    WeatherForecastList = Guard.Against.Null(weatherForecastList);
  }
}
