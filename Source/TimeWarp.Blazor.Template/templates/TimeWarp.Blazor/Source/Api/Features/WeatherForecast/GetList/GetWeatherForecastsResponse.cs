namespace TimeWarp.Architecture.Features.WeatherForecasts;

using System;
using System.Collections.Generic;

public record GetWeatherForecastsResponse : BaseResponse
{
  /// <summary>
  /// The collection of forecasts requested
  /// </summary>
  public List<WeatherForecastDto> WeatherForecasts { get; set; }

  ///// <summary>
  ///// a default constructor is required for client side deserialization
  ///// </summary>
  //public GetWeatherForecastsResponse()
  //{
  //  WeatherForecasts = new List<WeatherForecastDto>();
  //}

  public GetWeatherForecastsResponse(Guid aCorrelationId) : base(aCorrelationId)
  {
    WeatherForecasts = new List<WeatherForecastDto>();
  }

}
