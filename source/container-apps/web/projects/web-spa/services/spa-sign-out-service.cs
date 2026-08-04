#region Purpose
// SPA sign-out that matches the active auth registration: Entra/MSAL vs identity-session vs mock.
#endregion

#region Design
// Task 104-034: Profile menu used NavigateToLogout("authentication/logout") unconditionally,
// which renders RemoteAuthenticatorView and requires IRemoteAuthenticationService — unregistered
// when UseEntra is false (default after 104-021). This service branches:
//   UseEntra → NavigateToLogout (existing MSAL path + Authentication.razor)
//   else → POST api/identity/session/end (clears identity-session cookie), notify auth state,
//          navigate to /Login without RemoteAuthenticatorView
// Mock path has no server cookie of value; EndBrowserSession is still called (idempotent) and
// forceLoad ensures WASM auth state reloads anonymous.
// IConfiguration is injected so Profile.razor does not re-parse Authentication:* keys.
#endregion

namespace TimeWarp.Architecture.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Features.Account;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;

/// <summary>Mode-aware SPA sign-out for the profile menu and future callers.</summary>
public sealed class SpaSignOutService
{
  private readonly NavigationManager NavigationManager;
  private readonly IConfiguration Configuration;
  private readonly IWebServerApiService ApiService;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;

  public SpaSignOutService(
    NavigationManager navigationManager,
    IConfiguration configuration,
    IWebServerApiService apiService,
    AuthenticationStateProvider authenticationStateProvider)
  {
    NavigationManager = navigationManager;
    Configuration = configuration;
    ApiService = apiService;
    AuthenticationStateProvider = authenticationStateProvider;
  }

  public async Task SignOutAsync(CancellationToken cancellationToken = default)
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
      // Network/prerender failure: still leave the client signed-out UX path.
    }

    if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
    {
      identitySession.NotifySessionChanged();
    }
    else
    {
      // Mock (or any other provider): force a full reload so auth state is re-read anonymous.
      NavigationManager.NavigateTo(LoginPage.GetPageUrl(), forceLoad: true);
      return;
    }

    NavigationManager.NavigateTo(LoginPage.GetPageUrl());
  }
}
