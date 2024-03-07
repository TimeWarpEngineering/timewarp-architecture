namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

internal partial class WeatherForecastsState
{

  public override WeatherForecastsState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    var jsonSerializerOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    string json = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(WeatherForecasts))].ToString() ?? throw new InvalidOperationException();

    var newWeatherForecastsState = new WeatherForecastsState()
    {
      WeatherForecastList = JsonSerializer.Deserialize<List<WeatherForecastDto>>(json, jsonSerializerOptions) ?? throw new InvalidOperationException(),
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
    };

    return newWeatherForecastsState;
  }

  internal void Initialize(List<WeatherForecastDto> aWeatherForecastList)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    WeatherForecastList = Guard.Argument(aWeatherForecastList, nameof(aWeatherForecastList)).NotNull();
  }
}
