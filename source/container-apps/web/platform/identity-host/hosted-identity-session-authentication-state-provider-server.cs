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
// Task 205-003: API loopback (Profile PUT) is a separate hole — IdentitySessionCookieForwardingHandler
// copies the inbound Cookie onto the named WebService HttpClient so InteractiveServer/Auto
// [EndpointAuthorize] calls authenticate. Task 212: the same handler copies Host so passkey
// RP-ID selection matches the browser origin. This type only fixes CascadingAuthenticationState.
// Task 253: the cookie principal carries timewarp:principal_id but not the account-fingerprint
// claim the WASM path projects from GetCurrentSession, so the prerender path adds it (a separate
// unauthenticated ClaimsIdentity on a cloned principal — HttpContext.User itself is not mutated),
// computed with the same PrincipalFingerprint the session response uses.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using TimeWarp.Architecture.Configuration;

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
      return Task.FromResult(new AuthenticationState(WithAccountFingerprint(httpUser)));
    }

    return base.GetAuthenticationStateAsync();
  }

  private static ClaimsPrincipal WithAccountFingerprint(ClaimsPrincipal user)
  {
    if (user.HasClaim(claim => claim.Type == AccountFingerprintClaimType))
    {
      return user;
    }

    string? principalIdValue = user.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(principalIdValue, out Guid principalGuid) || principalGuid == Guid.Empty)
    {
      return user;
    }

    ClaimsPrincipal projected = user.Clone();
    projected.AddIdentity(new ClaimsIdentity(
      [new Claim(AccountFingerprintClaimType, PrincipalFingerprint.Compute(PrincipalId.From(principalGuid)))]));
    return projected;
  }
}
