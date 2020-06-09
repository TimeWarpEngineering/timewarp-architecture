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
        Days = 5
      };

      ValidationResult validationResult = __RequestName__RequestValidator.TestValidate(__requestName__Request);

      validationResult.IsValid.Should().BeTrue();
    }

    public void Have_error_when_Days_are_negative() => __RequestName__RequestValidator
      .ShouldHaveValidationErrorFor(a__RequestName__Request => a__RequestName__Request.Days, -1);

    public void Setup() => __RequestName__RequestValidator = new __RequestName__RequestValidator();
  }
}
