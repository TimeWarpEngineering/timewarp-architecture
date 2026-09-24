#region Purpose
// SignOutActionSet: user-initiated sign-out through the TimeWarp.State pipeline (profile menu).
#endregion

#region Design
// UX rule: user actions dispatch state actions — they must not call Spa services that side-effect
// auth/navigation outside the pipeline (task 104-034 follow-up).
// Task 251: sign-out is a BROWSER request, identical in WebAssembly, Server and Auto:
//   1. Reset ProfileState, AuthorizationState, CredentialsState, AgentLinksState (signed-out
//      chrome for the instant before the page unloads, and no stale data if the navigation fails)
//   2. SignOutJsModule: the browser fetches an antiforgery token and form-POSTs
//      SignOutBrowserSession.Path; the server ends the session (EndBrowserSession.Handler),
//      returns the expired identity-session Set-Cookie to the browser, and 303s to /Login.
// Why not the old IWebServerApiService POST + NotifySessionChanged + soft NavigateTo: under
// InteractiveServer that POST is a server→server loopback (cookie forwarded by
// IdentitySessionCookieForwardingHandler), so the cookie deletion landed on the HttpClient
// response, the hosted AuthenticationStateProvider was never re-evaluated, and the soft
// navigation kept the circuit's signed-in principal. The full navigation supersedes all three:
// the next page load authenticates from the browser's (now absent) cookie in every mode, so no
// provider notification is needed.
// Fallback: if the JS step throws (token fetch failed), forceLoad /Login — a real page load that
// shows whatever the browser cookie still says rather than a fake signed-out UI.
// Entra is a named BFF scheme, not a WASM MSAL session (RFC 219 D10) — no NavigateToLogout.
// AuthorizationState.Initialize is the same body as ClearCurrentUserActionSet — call via Store
// to avoid nested Action type references under TWA0009.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;

partial class ProfileState
{
  public static class SignOutActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

    // Opt-out must sit on the containing type of the reference (Handler), not outer ProfileState —
    // TWA0009 walks the innermost type declaration (see SliceIsolationAnalyzer.GetContainingType).
    [CrossSliceReference(typeof(AuthorizationState), "Sign-out resets role/permission cache with profile chrome (same Initialize as ClearCurrentUser).")]
    [CrossSliceReference(typeof(CredentialsState), "Sign-out clears credential list with profile chrome.")]
    [CrossSliceReference(typeof(AgentLinksState), "Sign-out clears agent-human links with profile chrome.")]
    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IJSRuntime JsRuntime;
      private readonly NavigationManager NavigationManager;

      public Handler(
        IStore store,
        IJSRuntime jsRuntime,
        NavigationManager navigationManager)
        : base(store)
      {
        JsRuntime = jsRuntime;
        NavigationManager = navigationManager;
      }

      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileState.Initialize();
        Store.GetState<AuthorizationState>().Initialize();
        Store.GetState<CredentialsState>().Initialize();
        Store.GetState<AgentLinksState>().Initialize();

        try
        {
          await SignOutJsModule.SignOutAsync(JsRuntime, cancellationToken);
        }
        catch (JSDisconnectedException)
        {
          // Server: the form submit unloaded the page and dropped the circuit — sign-out proceeds.
        }
        catch (JSException)
        {
          NavigationManager.NavigateTo(SignOutBrowserSession.RedirectPath, forceLoad: true);
        }
      }
    }
  }
}
