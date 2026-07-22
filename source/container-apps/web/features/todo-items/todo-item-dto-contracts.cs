#region Purpose
// Entity-centric DTO for todo items, kept as a reference point for the mixin-based contract-generation idea.
#endregion

#region Design
// The endpoint-centric pattern (dedicated Request/Response per endpoint) supersedes this entity-shaped
// DTO — see the TODO above the class.
// The commented attribute list sketches how mixins could generate CRUD contracts from one DTO; that
// approach was not adopted.
// Joe is a scratch type exercising primary-constructor + init-property syntax, not part of the contract surface.
#endregion

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
