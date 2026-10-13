#region Purpose
// Registers the Admin/Principals route and authorize policy; markup and behavior live in PrincipalsPage.razor.
#endregion

#region Design
// Task 147-004 D9 / 182-003: list of principals, policy = admin.principals.read. Task 328 turned
// the table into a FluentDataGrid with server-side search and sort (the task 327 pattern):
// Items is the state's rows and RefreshItems, which replaces the grid's own sort, maps the grid's
// primary sort column (ColumnId = PrincipalListSortColumn name) to SetSort then FetchPrincipals.
// It returns early when the sort already matches state, so the first render and new rows
// dispatch nothing. Each sortable column's IsDefaultSortColumn and InitialSortDirection follow
// state. Loading binds the grid to the fetch action, so the Section has no LoadingActionType
// (a spinner swap would destroy the grid and its sort state). GridItems is a new IQueryable per
// render on purpose, as on the feedback list, so the grid re-reads rows after a fetch.
// Display name and Email columns always render ("—" when the server sends null: no permission,
// an agent, or no profile) but are Sortable, and the search box shown, only for callers with
// admin.profiles.read (CheckAuthorization at init + AuthorizeView): the server answers 403 to
// profile searches or sorts without it.
// Role editing moved to the detail page (PrincipalDetailPage). Per-row checkbox grids do not fit a
// data grid, and one edit surface is easier to keep correct (draft per principal, one Save, a
// last-admin 409 reported where the edit happened).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals", Policy = PermissionIds.AdminPrincipalsRead, Navigable = true, Description = "List principals an administrator can inspect.")]
[Authorize(Policy = PermissionIds.AdminPrincipalsRead)]
partial class PrincipalsPage;
