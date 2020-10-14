//#WeatherForecast #GetWeatherForecasts #Response #Api
namespace TimeWarp.Blazor.Features.WeatherForecasts
{
  using System;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Features.Bases;

  public class GetWeatherForecastsResponse : BaseResponse
  {
    /// <summary>
    /// The collection of forecasts requested
    /// </summary>
    public List<WeatherForecastDto> WeatherForecasts { get; set; }

    /// <summary>
    /// a default constructor is required for client side deserialization
    /// </summary>
    public GetWeatherForecastsResponse()
    {
      WeatherForecasts = new List<WeatherForecastDto>();
    }

    public GetWeatherForecastsResponse(Guid aCorrelationId) : this()
    {
      CorrelationId = aCorrelationId;
    }
  }
}
