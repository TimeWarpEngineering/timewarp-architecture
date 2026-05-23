namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

partial class WeatherForecastsState
{

  public override WeatherForecastsState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    JsonSerializerOptions jsonSerializerOptions = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    string json = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(WeatherForecasts))].ToString() ?? throw new InvalidOperationException();

    var newWeatherForecastsState = new WeatherForecastsState()
    {
      WeatherForecastList = JsonSerializer.Deserialize<List<TWeatherForecast>>(json, jsonSerializerOptions) ?? throw new InvalidOperationException(),
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
