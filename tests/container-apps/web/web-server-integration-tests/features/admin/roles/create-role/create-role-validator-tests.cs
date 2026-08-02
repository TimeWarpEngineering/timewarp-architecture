#region Purpose
// Validator tests: the contract's composed rules (shared RoleDetailsValidator + auth) hold.
#endregion

namespace CreateRoleRequestValidator_;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Validate_Should
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Should>();

  public static Task Be_Valid_Given_Complete_Command()

  {
    var command = new Command
    {
      UserId = Guid.NewGuid(),
      Name = "Auditor",
      Description = "Read-only access."
    };

    ValidationResult validationResult = new Validator().TestValidate(command);

    validationResult.IsValid.ShouldBeTrue();

    return Task.CompletedTask;

  }

  public static Task Have_Error_When_Name_Is_Empty()

  {
    TestValidationResult<Command> result =
      new Validator().TestValidate(new Command { UserId = Guid.NewGuid(), Name = "", Description = "x" });

    result.ShouldHaveValidationErrorFor(command => command.Name);

    return Task.CompletedTask;

  }

  public static Task Have_Error_When_UserId_Is_Empty()

  {
    TestValidationResult<Command> result =
      new Validator().TestValidate(new Command { Name = "Auditor", Description = "x" });

    result.ShouldHaveValidationErrorFor(command => command.UserId);

    return Task.CompletedTask;

  }

}
