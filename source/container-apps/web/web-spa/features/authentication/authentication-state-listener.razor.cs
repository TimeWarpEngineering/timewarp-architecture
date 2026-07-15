#region Purpose
// Code-behind for AuthenticationStateListener: loads or clears profile/authorization state on auth changes.
#endregion

#region Design
// Identity sign-in/out is a deliberate multi-slice edge: Authentication owns the listener, Profiles
// owns profile cache, Authorization owns the current-user/roles cache. Documented via
// CrossSliceReference so TWPA0009 sees the coupling (razor @code alone is not analyzed).
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

[CrossSliceReference(typeof(ProfileState), "Identity pipeline: on sign-in load the profile for the principal.")]
[CrossSliceReference(typeof(AuthorizationState), "Identity pipeline: on sign-out clear authorization/current-user cache with profile.")]
partial class AuthenticationStateListener
{
  protected override bool ShouldRender()
  {
    // This component should only render once. Given there is no UX.
    return false;
  }

  protected override async Task OnInitializedAsync()
  {
    AuthenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
    AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    await HandleUserAuthentication(authenticationState.User);
  }

  private async void HandleAuthenticationStateChanged(Task<AuthenticationState> task)
  {
    AuthenticationState authenticationState = await task;
    await HandleUserAuthentication(authenticationState.User);
  }

  private async Task HandleUserAuthentication(ClaimsPrincipal user)
  {
    if (user.Identity?.IsAuthenticated == true)
    {
      await NoSubProfileState.FetchProfileData();
    }
    else
    {
      await NoSubProfileState.ClearProfileData();
      await NoSubAuthorizationState.ClearCurrentUser();
    }
  }

  public override void Dispose()
  {
    // Unsubscribe to avoid memory leaks
    AuthenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
    base.Dispose();
    GC.SuppressFinalize(this);
  }
}
