namespace __RequestName__RequestValidator_
{
  using FluentAssertions;
  using FluentValidation.Results;
  using FluentValidation.TestHelper;
  using __RootNamespace__.Features.__FeatureName__s;

  public class Validate_Should
  {
    private __RequestName__RequestValidator __RequestName__RequestValidator { get; set; }

    public void Be_Valid()
    {
      var __requestName__Request = new __RequestName__Request
      {
        // Set Valid values here
        // Days = 5
      };

      ValidationResult validationResult = __RequestName__RequestValidator.TestValidate(__requestName__Request);

      validationResult.IsValid.Should().BeTrue();
    }

    // public void Have_error_when_Days_are_negative() => GetWeatherForecastsRequestValidator
    //  .ShouldHaveValidationErrorFor(aGetWeatherForecastsRequest => aGetWeatherForecastsRequest.Days, -1);

    public void Setup() => GetWeatherForecastsRequestValidator = new __RequestName__RequestValidator();
  }
}
