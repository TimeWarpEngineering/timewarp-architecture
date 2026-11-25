#region Purpose
// Registers the Admin/Feedback/{id} route and authorize policy; markup and behavior live in FeedbackAdminItemPage.razor.
#endregion

#region Design
// Not navigable: the list page is the entry. Read policy loads the thread; the manage controls
// sit inside an AuthorizeView on admin.feedback.manage and the endpoints enforce the same policy.
// The facts list shows the app version and page path recorded at filing (task 521), or
// "Not recorded" for items filed before them or without a page.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

[Page("/Admin/Feedback/{FeedbackItemId:Guid}", Policy = PermissionIds.AdminFeedbackRead)]
[Authorize(Policy = PermissionIds.AdminFeedbackRead)]
partial class FeedbackAdminItemPage;
