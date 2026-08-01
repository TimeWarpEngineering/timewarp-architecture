namespace Hello_Validator;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class Validate_Should
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Should>();

  public static Task Be_Valid()

  {
    var query = new Query
    {
      Name = "SomeEvent"
    };

    ValidationResult validationResult = new Validator().TestValidate(query);

    validationResult.IsValid.ShouldBeTrue();

    return Task.CompletedTask;

  }

  public static Task Have_error_when_Name_is_empty()

  {
    TestValidationResult<Query> result =
      new Validator().TestValidate(new Query { Name = "" });

    result.ShouldHaveValidationErrorFor(command => command.Name);

    return Task.CompletedTask;

  }

}
