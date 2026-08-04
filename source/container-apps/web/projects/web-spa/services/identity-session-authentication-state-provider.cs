#region Purpose
// SPA AuthenticationStateProvider for the default non-mock path: first-party identity-session
// (passkey cookie) via GetCurrentSession — no Entra/MSAL required.
#endregion

#region Design
// Task 104-021: when Authentication:UseMock is off and Authentication:UseEntra is off, the SPA
// still needs an AuthenticationStateProvider for CascadingAuthenticationState / AuthorizeView.
// This provider reads GET api/identity/session (cookie ambient auth) and projects PrincipalId
// into claims plus default product role Member (task 147-002). Failures and unauthenticated
// sessions yield an anonymous principal (no throw).
// Role claims use RoleIds Guids as strings so RolePolicyGrants / RequireRole match.
// Principal→role assignment store is 147-004; until then every passkey principal is Member only
// (no Developer/Admin demos unless mock or future assignment).
// NotifySessionChanged lets Login / passkey ceremony refresh Blazor auth state after cookie set.
// AuthenticationType is a stable SPA-local string (not the server scheme name) — server cookie
// auth remains on web-server; this only shapes client UI identity.
#endregion

namespace TimeWarp.Architecture.Services;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

/// <summary>
/// Projects the browser's identity-session cookie into Blazor <see cref="AuthenticationState"/>.
/// </summary>
public sealed class IdentitySessionAuthenticationStateProvider : AuthenticationStateProvider
{
  private const string AuthenticationType = "identity-session";

  private readonly IWebServerApiService ApiService;

  public IdentitySessionAuthenticationStateProvider(IWebServerApiService apiService)
  {
    ApiService = apiService;
  }

  public override async Task<AuthenticationState> GetAuthenticationStateAsync()
  {
    try
    {
      OneOf<GetCurrentSession.Response, FileResponse, SharedProblemDetails> result =
        await ApiService.GetResponse<GetCurrentSession.Response>(
          new GetCurrentSession.Query(),
          CancellationToken.None);

      if (result.IsT0
        && result.AsT0.IsAuthenticated
        && result.AsT0.PrincipalId is { } principalId)
      {
        string principalIdValue = principalId.ToString();
        List<Claim> claims =
        [
          new(ClaimTypes.NameIdentifier, principalIdValue),
          new("timewarp:principal_id", principalIdValue),
          // Default product role until principal-role store (147-004).
          new(ClaimTypes.Role, RoleIds.Member.ToString()),
        ];
        ClaimsIdentity identity = new(claims, AuthenticationType);
        return new AuthenticationState(new ClaimsPrincipal(identity));
      }
    }
    catch
    {
      // Prerender / API unavailable → anonymous (same UX as no session).
    }

    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
  }

  /// <summary>
  /// Call after a successful passkey ceremony so AuthorizeView and auth listeners re-read session.
  /// </summary>
  public void NotifySessionChanged() =>
    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
