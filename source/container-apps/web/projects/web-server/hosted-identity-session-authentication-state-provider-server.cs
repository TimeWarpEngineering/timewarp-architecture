#region Purpose
// Server-hosted AuthenticationStateProvider: prefer cookie HttpContext.User during prerender.
#endregion

#region Design
// Task 183: Web.Server composes SPA IdentitySessionAuthenticationStateProvider for CascadingAuthenticationState.
// That provider HTTP-calls GetCurrentSession via IWebServerApiService; the named HttpClient does not
// forward the browser's .timewarp.identity.session cookie on loopback, so prerender always saw an
// anonymous principal. AuthorizeRouteView then failed PermissionRequirement on every [Authorize]
// page and RedirectToLogin.NavigateTo during static SSR stack-overflowed the process (exit 134).
// Prefer ambient HttpContext.User when authenticated (cookie validated + PrincipalRoleClaimsTransformation
// already applied). PermissionRequirementHandler evaluates via principal_id + AuthenticationType —
// no permission claims required on the cookie principal. Fall back to session HTTP for edge cases
// without an HttpContext. NotifySessionChanged still works via base (passkey ceremony casts to
// IdentitySessionAuthenticationStateProvider).
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Hosted override of the SPA identity-session auth state provider for web-server prerender.
/// </summary>
public sealed class HostedIdentitySessionAuthenticationStateProvider
  : IdentitySessionAuthenticationStateProvider
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HostedIdentitySessionAuthenticationStateProvider(
    IWebServerApiService apiService,
    IHttpContextAccessor httpContextAccessor)
    : base(apiService)
  {
    HttpContextAccessor = httpContextAccessor
      ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  public override Task<AuthenticationState> GetAuthenticationStateAsync()
  {
    ClaimsPrincipal? httpUser = HttpContextAccessor.HttpContext?.User;
    if (httpUser?.Identity?.IsAuthenticated == true)
    {
      return Task.FromResult(new AuthenticationState(httpUser));
    }

    return base.GetAuthenticationStateAsync();
  }
}
