namespace TimeWarp.Blazor.Features.WeatherForecastsGrpc
{
  using ProtoBuf;
  using System;
  using System.Collections.Generic;
  using System.Runtime.Serialization;

  [DataContract]
  public class GetWeatherForecastsResponse
  {
    [DataMember(Order = 1)]
    public List<WeatherForecastDto> WeatherForecasts { get; set; } = new List<WeatherForecastDto>();

    [DataContract]
    public class WeatherForecastDto
    {
      [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
      public DateTime Date { get; set; }

      [DataMember(Order = 2)]
      public string Summary { get; set; }

      [DataMember(Order = 3)]
      public int TemperatureC { get; set; }

      public WeatherForecastDto() { }

      public WeatherForecastDto(DateTime aDate, string aSummary, int aTemperatureC)
      {
        Date = aDate;
        Summary = aSummary;
        TemperatureC = aTemperatureC;
      }
    }
  }
}
