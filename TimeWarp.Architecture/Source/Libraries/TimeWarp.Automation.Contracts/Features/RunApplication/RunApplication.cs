namespace TimeWarp.Automation.Features;

using FluentValidation.Results;

public static partial class RunApplication
{
  public sealed partial class Command : IRequest<OneOf<Response, ValidationResult, Exception>>
  {
    public string ApplicationPath { get; set; } = null!;
    public string? Arguments { get; set; }
  }

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.ApplicationPath)
        .NotEmpty();
    }
  }

  public sealed class Response
  {
    public int ProcessId { get; init; }
  }
}
