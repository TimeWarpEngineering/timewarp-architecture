#region Purpose
// Registers the Admin/Feedback route and authorize policy; markup and behavior live in FeedbackAdminListPage.razor.
#endregion

#region Design
// Policy = admin.feedback.read. Navigable: it is the admin entry to the feedback queue. Filter
// and paging are State actions; the page dispatches FetchFeedbackList after each one. Closed
// items are hidden until "Show closed" is checked (server rule: IncludeClosed false and no Status).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

[Page("/Admin/Feedback", Policy = PermissionIds.AdminFeedbackRead, Navigable = true, Description = "Review and manage user feedback.")]
[Authorize(Policy = PermissionIds.AdminFeedbackRead)]
partial class FeedbackAdminListPage;
