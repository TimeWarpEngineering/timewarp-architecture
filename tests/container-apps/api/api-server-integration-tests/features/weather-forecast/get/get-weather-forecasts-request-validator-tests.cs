namespace GetWeatherForecastRequestValidator_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Validate_Should
{
  private Validator Validator = new();

  public void Be_Valid()
  {
    var query = new Query
    {
      Days = 5
    };

    ValidationResult validationResult = Validator.TestValidate(query);

    validationResult.IsValid.ShouldBeTrue();
  }

  public void Have_error_when_Days_are_negative()
  {
    TestValidationResult<Query> result =
      Validator.TestValidate(new Query { Days = -1 });

    result.ShouldHaveValidationErrorFor(query => query.Days);
  }

  public void Setup() => Validator = new Validator();
}
