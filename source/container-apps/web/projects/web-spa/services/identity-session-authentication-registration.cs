#region Purpose
// Single registration site for the default SPA identity-session (passkey cookie) auth providers.
#endregion

#region Design
// Task 104-021: when mock is off and Entra is off, register IdentitySessionAuthenticationStateProvider
// + NoOpAccessTokenProvider so CascadingAuthenticationState and BaseApiService resolve without MSAL.
// Caller decides the branch; this type only mutates the collection when invoked.
// Task 183: web-server re-registers HostedIdentitySessionAuthenticationStateProvider after composing
// SPA services so prerender uses HttpContext.User (see web-server Program.ConfigureServices).
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers SPA providers for first-party identity-session authentication (no Entra).
/// </summary>
public static class IdentitySessionAuthenticationRegistration
{
  public static void AddSpaIdentitySessionAuthentication(IServiceCollection serviceCollection)
  {
    serviceCollection.AddScoped<AuthenticationStateProvider, IdentitySessionAuthenticationStateProvider>();
    serviceCollection.AddScoped<IAccessTokenProvider, NoOpAccessTokenProvider>();
  }
}
