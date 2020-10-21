namespace __RequestName__PC__RequestValidator_
{
  using FluentAssertions;
  using FluentValidation.Results;
  using FluentValidation.TestHelper;
  using __RootNamespace__.Features.__FeatureName__s;

  public class Validate_Should
  {
    private __RequestName__PC__RequestValidator __RequestName__PC__RequestValidator;

    public Validate_Should()
    {
      __RequestName__PC__RequestValidator = new __RequestName__PC__RequestValidator();
    }

    public void Be_Valid()
    {
      var __RequestName__CC__Request = new __RequestName__PC__Request
      {
        // Set Valid values here
        // #TODO
        SampleProperty = "sample"
      };

      ValidationResult validationResult = __RequestName__PC__RequestValidator.TestValidate(__RequestName__CC__Request);

      validationResult.IsValid.Should().BeTrue();
    }

    // #TODO Rename thie test and add tests for all validation rules
    public void Have_error_when_SampleProperty_is_empty() => __RequestName__RequestValidator
      .ShouldHaveValidationErrorFor(a__RequestName__PC__Request => a__RequestName__PC__Request.SampleProperty, string.Empty);

  }
}
