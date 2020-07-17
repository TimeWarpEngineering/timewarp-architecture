namespace GetWeatherForecastRequestValidator_
{
  using FluentAssertions;
  using FluentValidation.Results;
  using FluentValidation.TestHelper;
  using TimeWarp.Blazor.Features.WeatherForecasts;

  public class Validate_Should
  {
    private GetWeatherForecastsRequestValidator GetWeatherForecastsRequestValidator;

    public void Be_Valid()
    {
      var getWeatherForecastsRequest = new GetWeatherForecastsRequest
      {
        Days = 5
      };

      ValidationResult validationResult = GetWeatherForecastsRequestValidator.TestValidate(getWeatherForecastsRequest);

      validationResult.IsValid.Should().BeTrue();
    }

    public void Have_error_when_Days_are_negative() => GetWeatherForecastsRequestValidator
     .ShouldHaveValidationErrorFor(aGetWeatherForecastsRequest => aGetWeatherForecastsRequest.Days, -1);

    public void Setup() => GetWeatherForecastsRequestValidator = new GetWeatherForecastsRequestValidator();
  }
}
