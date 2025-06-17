namespace TimeWarp.Architecture.Features.TodoItems;

// TODO: Revist the Mixins now that we have established better patterns
// We will use Endpoint centric APIs not Entity Centric so this DTO will go away.
// [CreateCommand, UpdateCommand, DeleteCommand, GetQuery, GetListQuery]
public class TodoItemDto
{
  public Guid TodoItemId { get; set; }

  public Guid TodoListId { get; set; }

  public string Title { get; set; } = string.Empty;

  public bool Done { get; set; }

  public int Priority { get; set; }

  public string Note { get; set; } = string.Empty;
}

public partial class Joe
(
  int? Page = null,
  int? PageSize = null,
  string? SearchString = null
  )
{
  public int? Page { get; init; } = Page;
  public int? PageSize { get; init; } = PageSize;
  public string? SearchString { get; init; } = SearchString;
}
