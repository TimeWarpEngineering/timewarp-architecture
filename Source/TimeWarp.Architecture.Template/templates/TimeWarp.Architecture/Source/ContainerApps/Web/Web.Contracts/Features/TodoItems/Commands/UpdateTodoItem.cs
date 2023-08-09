namespace TimeWarp.Architecture.Features.TodoItems;

public static class UpdateTodoItem
{
  //[RouteMixin("api/TodoItems/{TodoItemId:Guid}", HttpVerb.Post)]
  public partial record Command: IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {
    public Guid TodoItemId { get; init; }

    public Guid TodoListId { get; init; }

    public string Title { get; init; } = string.Empty;

    public bool Done { get; init; }

    public int Priority { get; init; }

    public string Note { get; init; } = string.Empty;

    public const string RouteTemplate = "api/TodoItems/{TodoItemId:Guid}";
    public HttpVerb GetHttpVerb() => HttpVerb.Post;
    public string GetRoute() => FormattableString.Invariant($"api/TodoItems/{TodoItemId:Guid}");
  }

  public record Response : BaseResponse { }

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.Title).NotEmpty();
    }
  }
}
