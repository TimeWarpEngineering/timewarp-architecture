#region Purpose
// Client-to-server chat contract: request that a message be broadcast on the chat hub.
#endregion

#region Design
// Travels over SignalR, not HTTP — hence no [ApiRoute]; the transport endpoint is ChatHubConstants.Route.
// Shaped as IRequest<OneOf<Success, SharedProblemDetails>> so hub traffic reuses the same mediator
// pipeline (validation, problem-details failures) as HTTP contracts.
#endregion

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
