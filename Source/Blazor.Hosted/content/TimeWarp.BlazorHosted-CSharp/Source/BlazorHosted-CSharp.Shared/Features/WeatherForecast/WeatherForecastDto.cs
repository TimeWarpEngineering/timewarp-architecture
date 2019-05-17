namespace BlazorHosted_CSharp.Shared.Features.WeatherForecast
{
  using System;

  public class WeatherForecastDto
  {
    public WeatherForecastDto() { }
    public DateTime Date { get; set; }
    public string Summary { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
  }
}