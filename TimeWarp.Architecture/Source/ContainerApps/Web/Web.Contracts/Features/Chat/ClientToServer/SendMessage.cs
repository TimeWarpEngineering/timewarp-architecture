namespace TimeWarp.Architecture.Features.Chat;

public static partial class SendMessage
{
  public sealed class Command : IRequest<OneOf<Success, SharedProblemDetails>>
  {
    public string User { get; set; } = null!;
    public string Message { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.User)
          .NotEmpty();

      RuleFor(x => x.Message)
          .NotEmpty();
    }
  }
}
