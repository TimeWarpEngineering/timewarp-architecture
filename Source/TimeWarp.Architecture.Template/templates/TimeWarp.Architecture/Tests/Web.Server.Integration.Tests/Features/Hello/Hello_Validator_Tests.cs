namespace Hello_Validator;

using static TimeWarp.Architecture.Features.Hello.Contracts.Hello;

public class Validate_Should
{
  private readonly Validator Validator;

  public Validate_Should()
  {
    Validator = new Validator();
  }

  public void Be_Valid()
  {
    var query = new Query
    {
      Name = "SomeEvent"
    };

    ValidationResult validationResult = Validator.TestValidate(query);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_Name_is_empty()
  {
    TestValidationResult<Query> result =
      Validator.TestValidate(new Query { Name = "" });

    result.ShouldHaveValidationErrorFor(command => command.Name);
  }
}
