#region Purpose
// Code-behind for the todo items list page: declares the route; the markup delegates rendering to TodoItemList.
#endregion

namespace TimeWarp.Architecture.Features.ToDo;

[Page("/todoitems", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class TodoItemsPage;
