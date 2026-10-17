#region Purpose
// Registers the feedback list route and the authorize policy.
#endregion

#region Design
// Navigable so the shell and the Ctrl-K page list can offer it. Policy matches the three
// feedback endpoints (feedback.file.self). The detail route is a separate page and is not navigable.
// Screenshots (task 303-001): "Capture this page" dispatches ScreenshotState.CaptureForFeedback,
// which records the capture it took as ForFeedback (only that action sets it, so an agent capture
// landing during the click cannot be uploaded in its place), and the page uploads ForFeedback.
// "Attach capture of {path}" offers the latest capture from another route (the page the person
// came from). Both upload through UploadOneAsync, the same limits, validation and
// UploadFeedbackAttachment action as a picked or pasted file, and show the usual removable preview.
// Capture and upload stay two actions sequenced by the page: ScreenshotState (platform tier)
// cannot reach Feedback's upload, a handler never dispatches another action, and the client-side
// limits, preview and markdown link live in UploadOneAsync, shared with paste and the file picker.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[Page("/Feedback", Policy = PermissionIds.FeedbackFileSelf, Navigable = true, Description = "File feedback and review the filings you submitted.", NavOrder = 40, AgentActions = [typeof(FeedbackState.SubmitFeedbackActionSet.Action), typeof(FeedbackState.ListMyFeedbackActionSet.Action), typeof(FeedbackState.OpenFeedbackActionSet.Action)])]
[Authorize(Policy = PermissionIds.FeedbackFileSelf)]
partial class FeedbackListPage;
