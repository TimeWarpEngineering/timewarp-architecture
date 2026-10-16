#region Purpose
// MarkAskSent: the composer's screenshot went out with a message; clear the thumbnail.
#endregion

#region Design
// Dispatched by AskScreenshotChatClient once the request that carried the picture succeeded,
// with the id it sent. A capture attached meanwhile (another id) stays for the next message.
// AskSentId lets page_context.screenshot say the conversation has seen that capture.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ScreenshotState
{
  public static class MarkAskSentActionSet
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
        ScreenshotState.Rebase(AgentSurfaceState.ConversationGeneration);
        ScreenshotState.AskSentId = action.CaptureId;
        if (ScreenshotState.AskAttached?.Id == action.CaptureId)
        {
          ScreenshotState.AskAttached = null;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
