#region Purpose
// Contract for deleting a todo item by id.
#endregion

#region Design
// [ApiRoute] drives source generation of the route members (hence partial); TodoItemId comes
// from the {TodoItemId:guid} route segment. [ClientOnlyContract] matches create/update: the
// todo-items server slice awaits the feature's finish-vs-delete decision.
#endregion

namespace TimeWarp.Architecture.Features.TodoItems;

public sealed partial class DeleteTodoItem
{
  [ApiRoute("api/TodoItems/{TodoItemId:guid}", HttpVerb.Delete)]
  [ClientOnlyContract("Todo items are a client demo; the server slice awaits the feature's finish-vs-delete decision.")]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest;

  public class Response : BaseResponse;

  public sealed partial class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.TodoItemId).NotEmpty();
    }
  }
}
