#region Purpose
// SignOutActionSet: user-initiated sign-out through the TimeWarp.State pipeline (profile menu).
#endregion

#region Design
// UX rule: user actions dispatch state actions — they must not call Spa services that side-effect
// auth/navigation outside the pipeline (task 104-034 follow-up).
// Flow:
//   1. POST EndBrowserSession (clear identity-session cookie). Entra is a named BFF scheme,
//      not a WASM MSAL session (RFC 219 D10) — no NavigateToLogout.
//   2. Reset ProfileState + AuthorizationState (signed-out chrome: avatar/alias + grants)
//   3. Notify IdentitySessionAuthenticationStateProvider so AuthorizeView flips to Sign-in
//   4. Soft-navigate to /Login (no forceLoad — state already cleared in-process)
// AuthorizationState.Initialize is the same body as ClearCurrentUserActionSet — call via Store
// to avoid nested Action type references under TWA0009. Route string "/Login" avoids Account slice.
// AuthenticationStateListener (Routes) remains passive path for non-UX auth changes.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;

partial class ProfileState
{
  internal static class SignOutActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    // Opt-out must sit on the containing type of the reference (Handler), not outer ProfileState —
    // TWA0009 walks the innermost type declaration (see SliceIsolationAnalyzer.GetContainingType).
    [CrossSliceReference(typeof(AuthorizationState), "Sign-out resets role/permission cache with profile chrome (same Initialize as ClearCurrentUser).")]
    [CrossSliceReference(typeof(CredentialsState), "Sign-out clears credential list with profile chrome.")]
    [CrossSliceReference(typeof(AgentLinksState), "Sign-out clears agent-human links with profile chrome.")]
    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IWebServerApiService ApiService;
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private readonly NavigationManager NavigationManager;

      public Handler(
        IStore store,
        IWebServerApiService apiService,
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
        : base(store)
      {
        ApiService = apiService;
        AuthenticationStateProvider = authenticationStateProvider;
        NavigationManager = navigationManager;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        try
        {
          _ = await ApiService.GetResponse<EndBrowserSession.Response>(
            new EndBrowserSession.Command(),
            cancellationToken);
        }
        catch
        {
          // Still clear client state so the UI is signed-out even if the network call fails.
        }

        ProfileState.Initialize();
        Store.GetState<AuthorizationState>().Initialize();
        Store.GetState<CredentialsState>().Initialize();
        Store.GetState<AgentLinksState>().Initialize();

        if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
        {
          identitySession.NotifySessionChanged();
        }

        NavigationManager.NavigateTo("/Login");
      }
    }
  }
}
