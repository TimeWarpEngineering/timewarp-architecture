#region Purpose
// Registers the Admin/Feedback route and authorize policy; markup and behavior live in FeedbackAdminListPage.razor.
#endregion

#region Design
// Policy = admin.feedback.read. Navigable: it is the admin entry to the feedback queue. Filter
// and paging are State actions; the page dispatches FetchFeedbackList after each one. Closed
// items are hidden until "Show closed" is checked (server rule: IncludeClosed false and no Status).
// The list is a FluentDataGrid with server-side sort (task 327): Items is the state's page and
// RefreshItems, which replaces the grid's own sort-and-page, maps the grid's primary sort column
// (ColumnId = FeedbackListSortColumn name) to SetSort then FetchFeedbackList. It returns early
// when the sort already matches state, so the first render and new rows dispatch nothing. Each
// sortable column's IsDefaultSortColumn and InitialSortDirection follow state, so a grid that
// is created again (revisiting the page, or after a load error) shows the stored sort. Owner is
// not sortable (task 336 decision: the name lives in the profile slice and the store pages in
// the database; see FeedbackListSortColumn). The Owner cell is FeedbackOwnerLink: the display
// name when the server sent one, else the id, linked to /Admin/Principals/{id}. Paging stays on Previous/Next: FluentPaginator drives the
// grid's own PaginationState, which would be a second copy of Page beside FeedbackAdminState.
// GridItems is a new IQueryable on every page render on purpose: the grid only re-reads rows
// when the Items reference changes, and the state instance is cloned before the fetch handler
// fills it, so caching per state instance would pin the empty pre-fetch rows. Page renders
// follow state changes only, and an unchanged sort does not call RefreshItems, so this costs
// one grid render per state change, not a loop.
// Loading binds the grid to the fetch action, so the Feedback Section has no LoadingActionType:
// Section swaps its content for a spinner, which would destroy the grid and its sort state.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

[Page("/Admin/Feedback", Policy = PermissionIds.AdminFeedbackRead, Navigable = true, Description = "Review and manage user feedback.")]
[Authorize(Policy = PermissionIds.AdminFeedbackRead)]
partial class FeedbackAdminListPage;
