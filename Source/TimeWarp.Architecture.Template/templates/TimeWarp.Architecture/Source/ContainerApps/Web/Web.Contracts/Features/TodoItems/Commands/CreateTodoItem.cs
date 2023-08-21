namespace TimeWarp.Architecture.Features.TodoItems;

public static class CreateTodoItem
{
  // TODO: Use Mixin to make this cleaner [RouteMixin("api/TodoItems", HttpVerb.Post)]
  public sealed partial class Command : IRequest<OneOf<Response, SharedProblemDetails>>, IApiRequest
  {

    public int ListId { get; init; }

    public string Title { get; init; } = string.Empty;

    public int Priority { get; init; }

    public string Note { get; init; } = string.Empty;

    public const string RouteTemplate = "api/TodoItems";
    public HttpVerb GetHttpVerb() => HttpVerb.Post;
    public string GetRoute() => FormattableString.Invariant($"api/TodoItems");
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
