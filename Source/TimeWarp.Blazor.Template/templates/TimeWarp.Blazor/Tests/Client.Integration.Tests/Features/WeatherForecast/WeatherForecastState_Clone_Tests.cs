namespace WeatherForecastsState
{
  using AnyClone;
  using Shouldly;
  using System;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Features.WeatherForecasts;

  public class Clone_Should : BaseTest
  {
    private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

    public Clone_Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      //Arrange
      var weatherForecasts = new List<WeatherForecastDto> {
        new WeatherForecastDto
        (
          aDate: DateTime.MinValue,
          aSummary: "Summary 1",
          aTemperatureC: 24
        ),
        new WeatherForecastDto
        (
          aDate: new DateTime(2019,05,17),
          aSummary: "Summary 1",
          aTemperatureC: 24
        )
      };
      WeatherForecastsState.Initialize(weatherForecasts);

      //Act
      var clone = WeatherForecastsState.Clone() as WeatherForecastsState;

      //Assert
      WeatherForecastsState.ShouldNotBeSameAs(clone);
      WeatherForecastsState.WeatherForecasts.Count.ShouldBe(clone.WeatherForecasts.Count);
      WeatherForecastsState.Guid.ShouldNotBe(clone.Guid);
      WeatherForecastsState.WeatherForecasts[0].TemperatureC.ShouldBe(clone.WeatherForecasts[0].TemperatureC);
      WeatherForecastsState.WeatherForecasts[0].ShouldNotBe(clone.WeatherForecasts[0]);
    }
  }
}
