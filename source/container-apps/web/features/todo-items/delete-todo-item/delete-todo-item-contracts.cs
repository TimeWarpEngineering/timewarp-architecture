#region Purpose
// Contract for deleting a todo item by id.
#endregion

#region Design
// Implements GetHttpVerb/GetRoute by hand instead of [ApiRoute], documenting the manual IApiRequest
// alternative to source generation.
// GetRoute returns the template with the {TodoItemId} token unexpanded — substitution is left to the sender.
#endregion

namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public sealed partial class DeleteTodoItem
{
  public sealed class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public const string Route = "TodoItems/{TodoItemId}";
    public Guid TodoItemId { get; set; }

    public HttpVerb GetHttpVerb() => HttpVerb.Delete;
    public string GetRoute() => $"{Route}";
  }

  public class Response : BaseResponse;

  public sealed partial class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.TodoItemId).NotEmpty();
    }
  }
}
