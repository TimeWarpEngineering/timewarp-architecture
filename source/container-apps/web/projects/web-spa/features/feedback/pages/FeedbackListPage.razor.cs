#region Purpose
// Registers the feedback list route and the authorize policy.
#endregion

#region Design
// Navigable so the shell and the Ctrl-K page list can offer it. Policy matches the three
// feedback endpoints (feedback.file.self). The detail route is a separate page and is not navigable.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[Page("/Feedback", Policy = PermissionIds.FeedbackFileSelf, Navigable = true, Description = "File feedback and review the filings you submitted.")]
[Authorize(Policy = PermissionIds.FeedbackFileSelf)]
partial class FeedbackListPage;
