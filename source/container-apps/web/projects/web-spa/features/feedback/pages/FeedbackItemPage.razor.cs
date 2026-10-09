#region Purpose
// Registers the feedback permalink route. Not a nav destination.
#endregion

#region Design
// The list page is the navigable entry. This route is the permalink the receipt links to.
// Policy matches the read endpoint so another principal is stopped before the handler.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[Page("/Feedback/{FeedbackItemId:Guid}", Policy = PermissionIds.FeedbackFileSelf)]
[Authorize(Policy = PermissionIds.FeedbackFileSelf)]
partial class FeedbackItemPage;
