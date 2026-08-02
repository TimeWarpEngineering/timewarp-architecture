#region Purpose
// TWeatherForecast contract seam serialize/deserialize round-trip (no Aspire host).
#endregion

namespace TWeatherForecast_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should>();

  public static Task SerializeAndDeserialize()
  {
    JsonSerializerOptions jsonSerializerOptions = ContractSerializationDefaults.Options;
    var weatherForecast = new TWeatherForecast
    (
      date: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
      summary: "Summary 1",
      temperatureC: 24
    );

    string json = JsonSerializer.Serialize(weatherForecast, jsonSerializerOptions);

    TWeatherForecast parsed = JsonSerializer.Deserialize<TWeatherForecast>(json, jsonSerializerOptions)!;

    weatherForecast.TemperatureC.ShouldBe(parsed.TemperatureC);
    weatherForecast.Summary.ShouldBe(parsed.Summary);
    weatherForecast.Date.ShouldBe(parsed.Date);
    return Task.CompletedTask;
  }
}
