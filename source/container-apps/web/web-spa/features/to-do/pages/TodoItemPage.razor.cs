#region Purpose
// Code-behind for the single todo item page: binds the TodoItemId route parameter consumed by the edit form.
#endregion

namespace TimeWarp.Architecture.Features.ToDo;

[Page("/todoitems/{TodoItemId:Guid}")]
partial class TodoItemPage;
