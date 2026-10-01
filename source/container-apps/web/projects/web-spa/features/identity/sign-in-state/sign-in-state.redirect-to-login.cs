#region Purpose
// RedirectToLogin: full-page navigation to /Login carrying the blocked page as a safe ?returnUrl.
#endregion

#region Design
// Task 265: RedirectToLogin.razor (AuthorizeRouteView's NotAuthorized fallback) used to call
// NavigationManager.NavigateTo itself; components only dispatch (TWA0026), so the navigation lives
// here. forceLoad stays (task 183): the redirect is a real page load once interactive, never a
// nested render. The handler builds the URL with LoginPage.GetLoginUrl so the open-redirect guard
// sits where the navigation happens, whoever dispatches.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;

partial class SignInState
{
  public static class RedirectToLoginActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(string returnPath)
      {
        ReturnPath = returnPath;
      }

      /// <summary>The page the visitor was blocked from (base-relative, leading slash).</summary>
      public string ReturnPath { get; }
    }

    internal sealed class Handler(IStore store, NavigationManager navigationManager) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        navigationManager.NavigateTo(LoginPage.GetLoginUrl(action.ReturnPath), forceLoad: true);
        return default;
      }
    }
  }
}
