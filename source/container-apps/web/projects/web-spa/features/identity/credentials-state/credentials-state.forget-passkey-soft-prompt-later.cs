#region Purpose
// ForgetPasskeySoftPromptLater: remove the session "later" key so the next principal on this tab sees the passkey prompt.
#endregion

#region Design
// Inverse of DismissPasskeySoftPrompt(rememberForSession: true). AuthenticationStateListener
// dispatches it on sign-out (interactive render only — sessionStorage is JS interop); the
// browser-storage write lives here because components only dispatch (task 265, TWA0026).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  public static class ForgetPasskeySoftPromptLaterActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, ISessionStorageService sessionStorage) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken) =>
        await sessionStorage.RemoveItemAsync(PasskeySoftPrompt.LaterStorageKey, cancellationToken);
    }
  }
}
