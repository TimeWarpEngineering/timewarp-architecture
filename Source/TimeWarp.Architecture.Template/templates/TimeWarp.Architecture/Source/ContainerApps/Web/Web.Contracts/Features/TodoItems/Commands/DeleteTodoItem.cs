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

  public record Response : BaseResponse { }

  public sealed partial class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.TodoItemId).NotEmpty();
    }
  }
}
