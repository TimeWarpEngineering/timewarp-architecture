#region Purpose
// Contract for creating a todo item — the write-command sample for the endpoint-centric pattern.
#endregion

#region Design
// Command, Response, and Validator nest in one wrapper class so an endpoint's entire contract lives in a
// single file (the endpoint-centric rule).
// Response adds nothing beyond BaseResponse: creation success needs no payload, and OneOf already carries
// the failure channel.
#endregion

namespace TimeWarp.Architecture.Features.TodoItems;

public sealed partial class CreateTodoItem
{
  [ApiRoute("api/TodoItems", HttpVerb.Post)]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {

    public int ListId { get; init; }

    public string Title { get; init; } = null!;

    public int Priority { get; init; }

    public string Note { get; init; } = string.Empty;

  }
  public class Response : BaseResponse { }

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.Title).NotEmpty();
    }
  }
}
