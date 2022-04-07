namespace WeatherForecastsState;

using AnyClone;
using FluentAssertions;
using System;
using System.Collections.Generic;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

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
        date: DateTime.MinValue,
        summary: "Summary 1",
        temperatureC: 24
      ),
      new WeatherForecastDto
      (
        date: new DateTime(2019,05,17),
        summary: "Summary 1",
        temperatureC: 24
      )
    };
    WeatherForecastsState.Initialize(weatherForecasts);

    //Act
    WeatherForecastsState clone = WeatherForecastsState.Clone();

    //Assert
    WeatherForecastsState.Should().NotBeSameAs(clone);
    WeatherForecastsState.WeatherForecasts.Count.Should().Be(clone.WeatherForecasts.Count);
    WeatherForecastsState.Guid.Should().NotBe(clone.Guid);
    WeatherForecastsState.WeatherForecasts[0].TemperatureC.Should().Be(clone.WeatherForecasts[0].TemperatureC);
    WeatherForecastsState.WeatherForecasts[0].Should().Be(clone.WeatherForecasts[0]); //record type
    object.ReferenceEquals(WeatherForecastsState.WeatherForecasts[0], clone.WeatherForecasts[0]).Should().BeFalse(); // recored type by reference
    //clone.WeatherForecasts[0]).Should().NotBe(true);

  }
}
