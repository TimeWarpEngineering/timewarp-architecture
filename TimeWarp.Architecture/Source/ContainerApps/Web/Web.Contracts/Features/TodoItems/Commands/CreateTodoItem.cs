namespace TimeWarp.Architecture.Features.TodoItems;

public sealed partial class CreateTodoItem
{
  [RouteMixin("api/TodoItems", HttpVerb.Post)]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {

    public int ListId { get; init; }

    public string Title { get; init; } = string.Empty;

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
