#region Purpose
// Hides the chat-recording notice and remembers that in this browser.
#endregion

#region Design
// Dismissal is browser-local. It does not change whether the server records chats. The flag goes
// to local storage under AskPrivacyNotice.DismissedStorageKey (task 347) so a reload keeps it;
// LoadPrivacyNoticeDismissal reads it back. A storage failure (blocked or full storage) is
// swallowed: the notice is still hidden for this session.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.JSInterop;

partial class AgentSurfaceState
{
  public static class DismissPrivacyNoticeActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      ILocalStorageService localStorage
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.PrivacyNoticeDismissed = true;
        try
        {
          await localStorage.SetItemAsync(AskPrivacyNotice.DismissedStorageKey, true, cancellationToken);
        }
        catch (JSException)
        {
          // Session-only dismissal is the fallback when the browser refuses storage.
        }
      }
    }
  }
}
