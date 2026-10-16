#region Purpose
// AttachToAsk: put a capture on Ask's composer as a removable thumbnail.
#endregion

#region Design
// Ask's Screenshot button dispatches CaptureScreenshot, then this with the new capture's id.
// Only Latest can be attached, so a stale id (another capture landed meanwhile) is a no-op. The
// attachment belongs to the conversation that is current now and replaces any earlier one: a
// message carries at most one screenshot.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ScreenshotState
{
  public static class AttachToAskActionSet
  {
    public sealed class Action : IBaseAction
    {
      public int CaptureId { get; }

      public Action(int captureId)
      {
        CaptureId = captureId;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (ScreenshotState.Latest is not { } latest || latest.Id != action.CaptureId)
        {
          return ValueTask.CompletedTask;
        }

        ScreenshotState.Rebase(AgentSurfaceState.ConversationGeneration);
        ScreenshotState.AskAttached = latest;
        return ValueTask.CompletedTask;
      }
    }
  }
}
