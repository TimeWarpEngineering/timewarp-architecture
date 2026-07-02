#region Purpose
// Contract for updating an existing todo item.
#endregion

#region Design
// The item id rides the route ({TodoItemId:guid}) rather than the body, so the command carries only
// mutable fields.
// Validation mirrors CreateTodoItem: the Title invariant must hold on every write, not just creation.
#endregion

namespace TimeWarp.Architecture.Features.TodoItems;

public sealed partial class UpdateTodoItem
{
  [ApiRoute("api/TodoItems/{TodoItemId:guid}", HttpVerb.Post)]
  [ClientOnlyContract("Todo items are a client demo; the server slice awaits the feature's finish-vs-delete decision.")]
  public partial class Command: IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public Guid TodoListId { get; init; }

    public string Title { get; init; } = null!;

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
