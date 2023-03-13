namespace TimeWarp.Architecture.Features.TodoItems;

public partial record UpdateTodoItemCommand
{
  public Guid TodoItemId { get; init; }

  public Guid TodoListId { get; init; }

  public string Title { get; init; } = string.Empty;

  public bool Done { get; init; }

  public int Priority { get; init; }

  public string Note { get; init; } = string.Empty;
}
