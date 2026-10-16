#region Purpose
// Registers the feedback list route and the authorize policy.
#endregion

#region Design
// Navigable so the shell and the Ctrl-K page list can offer it. Policy matches the three
// feedback endpoints (feedback.file.self). The detail route is a separate page and is not navigable.
// Screenshots (task 303-001): "Capture this page" dispatches ScreenshotState.CaptureScreenshot and
// "Attach capture of {path}" offers the latest capture from another route (the page the person
// came from). Both upload through UploadOneAsync, the same limits, validation and
// UploadFeedbackAttachment action as a picked or pasted file, and show the usual removable preview.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[Page("/Feedback", Policy = PermissionIds.FeedbackFileSelf, Navigable = true, Description = "File feedback and review the filings you submitted.", NavOrder = 40, AgentActions = [typeof(FeedbackState.SubmitFeedbackActionSet.Action), typeof(FeedbackState.ListMyFeedbackActionSet.Action), typeof(FeedbackState.OpenFeedbackActionSet.Action)])]
[Authorize(Policy = PermissionIds.FeedbackFileSelf)]
partial class FeedbackListPage;
