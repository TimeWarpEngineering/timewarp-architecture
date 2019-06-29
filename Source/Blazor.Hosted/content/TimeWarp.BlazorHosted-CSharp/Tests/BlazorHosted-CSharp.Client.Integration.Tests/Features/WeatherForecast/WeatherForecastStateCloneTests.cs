namespace BlazorHosted_CSharp.Client.Integration.Tests.Features.WeatherForecast
{
  using AnyClone;
  using BlazorHosted_CSharp.Client.Features.WeatherForecast;
  using BlazorHosted_CSharp.Client.Integration.Tests.Infrastructure;
  using BlazorHosted_CSharp.Shared.Features.WeatherForecast;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using System.Collections.Generic;

  internal class WeatherForecastStateCloneTests
  {
    private WeatherForecastsState WeatherForecastsState { get; set; }

    public WeatherForecastStateCloneTests(TestFixture aTestFixture)
    {
      IServiceProvider serviceProvider = aTestFixture.ServiceProvider;
      IStore store = serviceProvider.GetService<IStore>();
      WeatherForecastsState = store.GetState<WeatherForecastsState>();
    }

    public void ShouldClone()
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