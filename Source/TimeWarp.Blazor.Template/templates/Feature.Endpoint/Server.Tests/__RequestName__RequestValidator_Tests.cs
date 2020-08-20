namespace __RequestName__RequestValidator_
{
  using FluentAssertions;
  using FluentValidation.Results;
  using FluentValidation.TestHelper;
  using __RootNamespace__.Features.__FeatureName__s;

  public class Validate_Should
  {
    private __RequestName__RequestValidator __RequestName__RequestValidator;

    public void Be_Valid()
    {
      var __RequestName__Request = new __RequestName__Request
      {
        // Set Valid values here
        SampleProperty = "sample"
      };

      ValidationResult validationResult = __RequestName__RequestValidator.TestValidate(__RequestName__Request);

      validationResult.IsValid.Should().BeTrue();
    }

    public void Have_error_when_SampleProperty_is_empty() => __RequestName__RequestValidator
      .ShouldHaveValidationErrorFor(a__RequestName__Request => a__RequestName__Request.SampleProperty, string.Empty);

    public void Setup() => __RequestName__RequestValidator = new __RequestName__RequestValidator();
  }
}
