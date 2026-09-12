#region Purpose
// Entity-centric DTO for todo items, kept as a reference against the adopted endpoint-centric contract shape.
#endregion

#region Design
// Endpoint-centric contracts (dedicated Request/Response per endpoint) supersede this entity-shaped
// DTO. The commented attribute list sketches generating CRUD contracts from one DTO; that approach
// was not adopted. Do not revive DTO-driven contract generation.
// Joe is a scratch type exercising primary-constructor + init-property syntax, not part of the contract surface.
#endregion

namespace TimeWarp.Architecture.Features.TodoItems;

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
