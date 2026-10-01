#region Purpose
// DismissPasskeySoftPrompt: hide the Entra-session add-passkey banner for this SPA session.
#endregion

#region Design
// The flag is in memory; RememberForSession also writes the "later" key to sessionStorage (the
// user's Later click) — browser storage is a side effect, so it lives in the handler, not the
// prompt. AddPasskeyPrompt's restore on first interactive render dispatches without it (the key
// is already there). Logout Initialize() resets the flag; AuthenticationStateListener removes the
// later key so a following principal on the same tab is not suppressed.
// RFC 219 D8: dismiss is UX, never a route or session gate.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  public static class DismissPasskeySoftPromptActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(bool rememberForSession = false)
      {
        RememberForSession = rememberForSession;
      }

      /// <summary>True to persist the dismissal for this browser tab (sessionStorage).</summary>
      public bool RememberForSession { get; }
    }

    internal sealed class Handler(IStore store, ISessionStorageService sessionStorage) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.PasskeySoftPromptDismissed = true;
        if (action.RememberForSession)
        {
          await sessionStorage.SetItemAsync(PasskeySoftPrompt.LaterStorageKey, true, cancellationToken);
        }
      }
    }
  }
}
