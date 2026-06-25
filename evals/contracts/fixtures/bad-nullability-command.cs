// Eval fixture: intentional nullability contradiction (legacy anti-pattern).
namespace MyApp.Features.Users;

public static partial class UpdateUser
{
  public sealed partial class Command
  {
    public string? Email { get; set; }
    public string? MandatoryNickname { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.Email).NotEmpty();
      RuleFor(x => x.MandatoryNickname).NotEmpty();
    }
  }
}