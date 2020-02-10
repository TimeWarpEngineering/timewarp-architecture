namespace TimeWarp.Blazor.Features.WeatherForecasts.Server.GetWeatherForecasts
{
  using System;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Features.Bases;

  public class GetWeatherForecastsResponse : BaseResponse
  {
    public List<WeatherForecastDto> WeatherForecasts { get; set; }

    /// <summary>
    /// a default constructor is required for deserialization
    /// </summary>
    public GetWeatherForecastsResponse() { }

    public GetWeatherForecastsResponse(Guid aRequestId)
    {
      WeatherForecasts = new List<WeatherForecastDto>();
      RequestId = aRequestId;
    }
  }
}
