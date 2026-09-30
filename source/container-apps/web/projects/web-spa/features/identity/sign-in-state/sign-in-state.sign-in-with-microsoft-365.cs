#region Purpose
// SignInWithMicrosoft365: full-page navigation to the BFF Entra challenge (mode=bootstrap) with a safe return URL.
#endregion

#region Design
// RFC 219 D10: Entra is a named BFF scheme, not a WASM MSAL session, so sign-in is a forceLoad
// navigation the server answers with the challenge redirect. The server reports "not offered"
// itself (404 when the scheme is not registered, 403 when site policy disables sign-in).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;

partial class SignInState
{
  public static class SignInWithMicrosoft365ActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(string returnPath)
      {
        ReturnPath = returnPath;
      }

      public string ReturnPath { get; }
    }

    internal sealed class Handler(IStore store, NavigationManager navigationManager) : BaseHandler<Action>(store)
    {
      internal const string Mode = "bootstrap";

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        navigationManager.NavigateTo
        (
          ChallengeEntra.GetRoute(Mode, LoginPage.GetSafeReturnUrl(action.ReturnPath)),
          forceLoad: true
        );
        return default;
      }
    }
  }
}
