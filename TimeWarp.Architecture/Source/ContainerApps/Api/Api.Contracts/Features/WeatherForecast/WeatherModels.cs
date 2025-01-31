namespace TimeWarp.Architecture.Features.WeatherForecasts;

/// <summary>
/// Weather forecast response
/// </summary>
public sealed class WeatherResponse
{
  /// <summary>
  /// The forecast for this Date
  /// </summary>
  /// <example>2020-06-08T12:32:39.9828696+07:00</example>
  public DateTime Date { get; init; }

  /// <summary>
  /// Summary of the forecast
  /// </summary>
  /// <example>Cool</example>
  public string Summary { get; init; }

  /// <summary>
  /// Temperature in Celsius
  /// </summary>
  /// <example>24</example>
  public int TemperatureC { get; init; }

  /// <summary>
  /// Temperature in Fahrenheit
  /// </summary>
  /// <example>75</example>
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

  public WeatherResponse
  (
    DateTime date,
    string summary,
    int temperatureC
  )
  {
    Date = date;
    Summary = summary;
    TemperatureC = temperatureC;
  }
}

/// <summary>
/// Weather forecast request
/// </summary>
public sealed class WeatherRequest
{
  /// <summary>
  /// The Number of days of forecasts to get
  /// </summary>
  /// <example>5</example>
  public int? Days { get; init; }
}
