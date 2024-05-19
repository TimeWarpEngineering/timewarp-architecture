namespace TimeWarp.Architecture.Features.TodoItems;

public sealed partial class UpdateTodoItem
{
  [RouteMixin("api/TodoItems/{TodoItemId:guid}", HttpVerb.Post)]
  public partial class Command: IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public Guid TodoListId { get; init; }

    public string Title { get; init; } = string.Empty;

    public bool Done { get; init; }

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
