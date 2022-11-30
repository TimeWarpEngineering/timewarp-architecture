namespace TimeWarp.Architecture.Features.WeatherForecasts;

public record GetWeatherForecastsResponse : BaseResponse
{
  /// <summary>
  /// The collection of forecasts requested
  /// </summary>
  public List<WeatherForecastDto> WeatherForecasts { get; set; }

  public GetWeatherForecastsResponse(List<WeatherForecastDto> weatherForecasts) :
    base()
  {
    WeatherForecasts = weatherForecasts;
  }
}
