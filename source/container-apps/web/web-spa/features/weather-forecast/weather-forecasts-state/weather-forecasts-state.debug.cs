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
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    WeatherForecastList = Guard.Against.Null(weatherForecastList);
  }
}
