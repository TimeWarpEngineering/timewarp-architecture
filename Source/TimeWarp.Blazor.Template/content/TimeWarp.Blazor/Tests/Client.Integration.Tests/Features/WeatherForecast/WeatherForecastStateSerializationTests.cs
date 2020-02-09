namespace TimeWarp.Blazor.Integration.Tests.Features.WeatherForecast
{
  using Shouldly;
  using System;
  using System.Text.Json;
  using TimeWarp.Blazor.Api.Features.WeatherForecast;

  internal class WeatherForecastStateSerializationTests
  {
    public void ShouldSerializeAndParse()
    {
      //Arrange
      var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
      var weatherForecastDto = new WeatherForecastDto
      (
        aDate: DateTime.MinValue.ToUniversalTime(),
        aSummary: "Summary 1",
        aTemperatureC: 24
      );

      string json = JsonSerializer.Serialize(weatherForecastDto, jsonSerializerOptions);

      //Act
      WeatherForecastDto parsed = JsonSerializer.Deserialize<WeatherForecastDto>(json, jsonSerializerOptions);

      //Assert
      weatherForecastDto.TemperatureC.ShouldBe(parsed.TemperatureC);
      weatherForecastDto.Summary.ShouldBe(parsed.Summary);
      weatherForecastDto.Date.ShouldBe(parsed.Date);
    }

    public void ShouldSerializeAndParseSample()
    {
      var jsonSerializerOptions = new JsonSerializerOptions();
      var person = new Person { FirstName = "Steve", LastName = "Cramer", BirthDay = new DateTime(1967, 09, 27) };
      string json = JsonSerializer.Serialize(person, jsonSerializerOptions);
      Person parsed = JsonSerializer.Deserialize<Person>(json, jsonSerializerOptions);
      parsed.BirthDay.ShouldBe(person.BirthDay);
      parsed.FirstName.ShouldBe(person.FirstName);
      parsed.LastName.ShouldBe(person.LastName);
    }

    public class Person
    {
      public DateTime? BirthDay { get; set; }
      public string FirstName { get; set; }
      public string LastName { get; set; }
    }
  }
}
