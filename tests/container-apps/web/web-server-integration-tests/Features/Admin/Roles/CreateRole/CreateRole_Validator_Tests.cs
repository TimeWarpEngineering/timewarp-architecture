#region Purpose
// Validator tests: the contract's composed rules (shared RoleDetailsValidator + auth) hold.
#endregion

namespace CreateRoleRequestValidator_;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Validate_Should
{
  private Validator Validator = new();

  public void Be_Valid_Given_Complete_Command()
  {
    var command = new Command
    {
      UserId = Guid.NewGuid(),
      Name = "Auditor",
      Description = "Read-only access."
    };

    ValidationResult validationResult = Validator.TestValidate(command);

    validationResult.IsValid.ShouldBeTrue();
  }

  public void Have_Error_When_Name_Is_Empty()
  {
    TestValidationResult<Command> result =
      Validator.TestValidate(new Command { UserId = Guid.NewGuid(), Name = "", Description = "x" });

    result.ShouldHaveValidationErrorFor(command => command.Name);
  }

  public void Have_Error_When_UserId_Is_Empty()
  {
    TestValidationResult<Command> result =
      Validator.TestValidate(new Command { Name = "Auditor", Description = "x" });

    result.ShouldHaveValidationErrorFor(command => command.UserId);
  }

  public void Setup() => Validator = new Validator();
}
