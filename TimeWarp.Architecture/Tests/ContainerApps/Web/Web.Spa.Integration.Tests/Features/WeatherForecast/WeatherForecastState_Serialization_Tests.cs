namespace WeatherForecastDto_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Should
{
  public void SerializeAndDeserialize()
  {
    //Arrange
    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var weatherForecastDto = new WeatherForecastDto
    (
      date: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
      summary: "Summary 1",
      temperatureC: 24
    );

    string json = JsonSerializer.Serialize(weatherForecastDto, jsonSerializerOptions);

    //Act
    WeatherForecastDto parsed = JsonSerializer.Deserialize<WeatherForecastDto>(json, jsonSerializerOptions);

    //Assert
    weatherForecastDto.TemperatureC.ShouldBe(parsed.TemperatureC);
    weatherForecastDto.Summary.ShouldBe(parsed.Summary);
    weatherForecastDto.Date.ShouldBe(parsed.Date);
  }
}
