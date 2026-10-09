#region Purpose
// Hides the chat-recording notice for this shell session.
#endregion

#region Design
// Dismissal is local shell state. It does not change whether the server records chats.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class DismissPrivacyNoticeActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.PrivacyNoticeDismissed = true;
        return default;
      }
    }
  }
}
