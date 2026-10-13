#region Purpose
// Restores a chat-recording notice dismissal saved in this browser.
#endregion

#region Design
// AgentAsk dispatches this on its first render, before it builds the agent, so the footer that
// holds the notice never paints a dismissed notice. It runs after render because local storage is
// JS interop and prerender has none. It only ever sets the flag: an absent or unreadable key
// leaves the notice as it is. Task 347.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.JSInterop;

partial class AgentSurfaceState
{
  public static class LoadPrivacyNoticeDismissalActionSet
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
        bool dismissed;
        try
        {
          dismissed = await localStorage.GetItemAsync<bool>(AskPrivacyNotice.DismissedStorageKey, cancellationToken);
        }
        catch (JSException)
        {
          return;
        }

        if (dismissed)
        {
          AgentSurfaceState.PrivacyNoticeDismissed = true;
        }
      }
    }
  }
}
