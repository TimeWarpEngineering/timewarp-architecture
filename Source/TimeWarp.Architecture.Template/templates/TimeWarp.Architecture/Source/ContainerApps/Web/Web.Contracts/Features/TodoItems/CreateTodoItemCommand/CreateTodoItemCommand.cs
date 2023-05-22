namespace TimeWarp.Architecture.Features.TodoItems;

[RouteMixin("api/TodoItems", HttpVerb.Post)]
public partial record CreateTodoItemCommand : IApiRequest
{
  public int ListId { get; init; }

  public string Title { get; init; } = string.Empty;

  public int Priority { get; init; }

  public string Note { get; init; } = string.Empty;
}
