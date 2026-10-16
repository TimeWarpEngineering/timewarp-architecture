#region Purpose
// CaptureForFeedback: Feedback's "Capture this page" button. Draws the page body and records that
// capture as ForFeedback, the one the Feedback page uploads.
#endregion

#region Design
// Mirrors CaptureForAsk: one action for one click, and ForFeedback is set only here, to the
// capture this handler took. An agent capture landing during the click changes Latest, never
// ForFeedback, so Feedback cannot upload the agent's picture as "this page".
// The drawing is ScreenshotState.CaptureAsync, shared with CaptureScreenshot and CaptureForAsk:
// one caller of ScreenshotJsModule, the same refusal notification and the same Latest.
// The upload is not here. Applications is the platform tier and cannot reach Feedback's
// UploadFeedbackAttachment, and a handler never dispatches another action, so the Feedback page
// uploads ForFeedback through its usual attachment path (FeedbackListPage Design).
// No selector: the page body. Opted out of the Ctrl-K palette, which already lists
// Capture screenshot.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ScreenshotState
{
  public static class CaptureForFeedbackActionSet
  {
    [CommandPaletteOptOut("Feedback's Capture this page button; the palette offers Capture screenshot.")]
    [TrackAction]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IJSRuntime jsRuntime,
      PageAgentRoute route,
      NavigationManager navigation,
      AgentCallOutcome agentCallOutcome,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CaptureServices services = new(jsRuntime, route, navigation, agentCallOutcome, publisher);
        if (await ScreenshotState.CaptureAsync("", services, cancellationToken) is { } capture)
        {
          ScreenshotState.ForFeedback = capture;
        }
      }
    }
  }
}
