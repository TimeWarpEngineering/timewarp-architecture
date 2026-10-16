#region Purpose
// RemoveFromAsk: take the screenshot thumbnail off Ask's composer before it is sent.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ScreenshotState
{
  public static class RemoveFromAskActionSet
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
        if (ScreenshotState.AskAttached?.Id == action.CaptureId)
        {
          ScreenshotState.AskAttached = null;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
