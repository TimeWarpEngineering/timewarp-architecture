#region Purpose
// SignOutActionSet: user-initiated sign-out through the TimeWarp.State pipeline (profile menu).
#endregion

#region Design
// UX rule: user actions dispatch state actions — they must not call Spa services that side-effect
// auth/navigation outside the pipeline (task 104-034 follow-up).
// Flow:
//   1. Entra (UseEntra) → MSAL NavigateToLogout (RemoteAuthenticatorView owns the rest)
//   2. Otherwise → POST EndBrowserSession (clear identity-session cookie)
//   3. Reset ProfileState + AuthorizationState (signed-out chrome: avatar/alias + grants)
//   4. Notify IdentitySessionAuthenticationStateProvider so AuthorizeView flips to Sign-in
//   5. Soft-navigate to /Login (no forceLoad — state already cleared in-process)
// AuthorizationState.Initialize is the same body as ClearCurrentUserActionSet — call via Store
// to avoid nested Action type references under TWA0009. Route string "/Login" avoids Account slice.
// AuthenticationStateListener (Routes) remains passive path for non-UX auth changes.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;

partial class ProfileState
{
  internal static class SignOutActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    // Opt-out must sit on the containing type of the reference (Handler), not outer ProfileState —
    // TWA0009 walks the innermost type declaration (see SliceIsolationAnalyzer.GetContainingType).
    [CrossSliceReference(typeof(AuthorizationState), "Sign-out resets role/module cache with profile chrome (same Initialize as ClearCurrentUser).")]
    [CrossSliceReference(typeof(CredentialsState), "Sign-out clears credential list with profile chrome.")]
    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IWebServerApiService ApiService;
      private readonly IConfiguration Configuration;
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private readonly NavigationManager NavigationManager;

      public Handler(
        IStore store,
        IWebServerApiService apiService,
        IConfiguration configuration,
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
        : base(store)
      {
        ApiService = apiService;
        Configuration = configuration;
        AuthenticationStateProvider = authenticationStateProvider;
        NavigationManager = navigationManager;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (MockAuthenticationDefaults.IsEntraAuthActive(
              Configuration[MockAuthenticationDefaults.UseEntraKey]))
        {
          NavigationManager.NavigateToLogout("authentication/logout");
          return;
        }

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

        if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
        {
          identitySession.NotifySessionChanged();
        }

        NavigationManager.NavigateTo("/Login");
      }
    }
  }
}
