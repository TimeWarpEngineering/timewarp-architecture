namespace WeatherForecastsState_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Clone_Should : BaseTest
{
  private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

  public Clone_Should
  (
    ISpaTestApplication aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public void Clone()
  {
    //Arrange
    var weatherForecasts = new List<WeatherForecastDto> {
      new WeatherForecastDto
      (
        date: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        summary: "Summary 1",
        temperatureC: 24
      ),
      new WeatherForecastDto
      (
        date: new DateTime(2019, 5, 17, 0, 0, 0, DateTimeKind.Utc),
        summary: "Summary 2",
        temperatureC: 25
      )
    };
    WeatherForecastsState.Initialize(weatherForecasts);

    //Act
    WeatherForecastsState clone = WeatherForecastsState.Clone();

    //Assert
    WeatherForecastsState.ShouldNotBeSameAs(clone);
    WeatherForecastsState.WeatherForecasts.Count.ShouldBe(clone.WeatherForecasts.Count);
    WeatherForecastsState.Guid.ShouldNotBe(clone.Guid);
    WeatherForecastsState.WeatherForecasts[0].TemperatureC.ShouldBe(clone.WeatherForecasts[0].TemperatureC);
    WeatherForecastsState.WeatherForecasts[0].ShouldBe(clone.WeatherForecasts[0]);           // WeatherForecastDTO is a `record class` thus equality should be true
    WeatherForecastsState.WeatherForecasts[0].ShouldNotBeSameAs(clone.WeatherForecasts[0]);  // record class is a reference type thus the reference should be different
  }
}
