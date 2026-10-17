#region Purpose
// CaptureForAsk: Ask's camera button. Draws the page body and puts that capture on Ask's composer.
#endregion

#region Design
// One action for one click, so the attach targets the capture this handler took, not whatever
// Latest is after the dispatch returns (an agent capture landing meanwhile used to be a race).
// The drawing is ScreenshotState.CaptureAsync, shared with CaptureScreenshot: still one caller of
// ScreenshotJsModule, the same refusal notification and the same Latest. On success the capture
// becomes AskAttached for the conversation that is current now and replaces any earlier one: a
// message carries at most one screenshot.
// No selector: the page body, which is the page behind the docked Ask panel. Opted out of the
// Ctrl-K palette: the palette already lists Capture screenshot, and this one belongs to the
// Ask composer's button.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ScreenshotState
{
  public static class CaptureForAskActionSet
  {
    [CommandPaletteOptOut("Ask's camera button; the palette offers Capture screenshot.")]
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
        if (await ScreenshotState.CaptureAsync("", services, cancellationToken) is not { } capture)
        {
          return;
        }

        ScreenshotState.Rebase(AgentSurfaceState.ConversationGeneration);
        ScreenshotState.AskAttached = capture;
      }
    }
  }
}
