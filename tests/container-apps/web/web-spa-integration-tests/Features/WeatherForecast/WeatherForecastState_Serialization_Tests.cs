namespace TWeatherForecast_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Should
{
  public static void SerializeAndDeserialize()
  {
    //Arrange
    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var weatherForecast = new TWeatherForecast
    (
      date: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
      summary: "Summary 1",
      temperatureC: 24
    );

    string json = JsonSerializer.Serialize(weatherForecast, jsonSerializerOptions);

    //Act
    TWeatherForecast parsed = JsonSerializer.Deserialize<TWeatherForecast>(json, jsonSerializerOptions);

    //Assert
    weatherForecast.TemperatureC.ShouldBe(parsed.TemperatureC);
    weatherForecast.Summary.ShouldBe(parsed.Summary);
    weatherForecast.Date.ShouldBe(parsed.Date);
  }
}
