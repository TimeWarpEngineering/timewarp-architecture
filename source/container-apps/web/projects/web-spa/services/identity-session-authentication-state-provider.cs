#region Purpose
// SPA AuthenticationStateProvider for the default non-mock path: first-party identity-session
// (passkey cookie) via GetCurrentSession — no Entra/MSAL required.
#endregion

#region Design
// Task 104-021: when Authentication:UseMock is off and Authentication:UseEntra is off, the SPA
// still needs an AuthenticationStateProvider for CascadingAuthenticationState / AuthorizeView.
// Reads GET api/identity/session (cookie ambient auth) and projects:
//   - PrincipalId → NameIdentifier + timewarp:principal_id
//   - Response.RoleIds → ClaimTypes.Role (diagnostics / UserClaims display; task 147-004 D4)
//   - Response.Permissions → PermissionIds.ClaimType claims (task 182-003) so SPA policies
//     registered via AddPermissionClaimPolicies can AuthorizeView without an evaluator in WASM
// Failures and unauthenticated sessions yield an anonymous principal (no throw).
// Empty RoleIds falls back to Member so a malformed/legacy payload still gets the product default.
// NotifySessionChanged lets Login / passkey ceremony refresh Blazor auth state after cookie set.
// AuthenticationType matches AuthenticationSchemeNames.IdentitySession so hosted
// PermissionRequirementHandler can pass the scheme into IPermissionEvaluator when this principal
// is used (WASM claim policies do not need the scheme).
//
// Task 183: not sealed — web-server registers HostedIdentitySessionAuthenticationStateProvider
// which prefers HttpContext.User during prerender (SPA HttpClient loopback cannot forward the
// identity-session cookie). Pure WASM still uses this type via IdentitySessionAuthenticationRegistration.
#endregion

namespace TimeWarp.Architecture.Services;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;

/// <summary>
/// Projects the browser's identity-session cookie into Blazor <see cref="AuthenticationState"/>.
/// </summary>
public class IdentitySessionAuthenticationStateProvider : AuthenticationStateProvider
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
        GetCurrentSession.Response session = result.AsT0;
        string principalIdValue = principalId.ToString();
        List<Claim> claims =
        [
          new(ClaimTypes.NameIdentifier, principalIdValue),
          new("timewarp:principal_id", principalIdValue),
        ];

        IEnumerable<Guid> roleIds = session.RoleIds is { Count: > 0 }
          ? session.RoleIds
          : [RoleIds.Member];
        foreach (Guid roleId in roleIds)
        {
          claims.Add(new Claim(ClaimTypes.Role, roleId.ToString()));
        }

        foreach (string permissionId in session.Permissions)
        {
          claims.Add(new Claim(PermissionIds.ClaimType, permissionId));
        }

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
