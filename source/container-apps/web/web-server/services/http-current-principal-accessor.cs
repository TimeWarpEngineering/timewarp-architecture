#region Purpose
// Scheme-agnostic ICurrentPrincipalAccessor implementation: reads the principal-id claim off the
// merged ClaimsPrincipal the authorization middleware attached to HttpContext.User.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), matching CookieBrowserSessionService/AgentCallerContext.
// Deliberately does NOT call HttpContext.AuthenticateAsync(scheme) for a specific scheme name (unlike
// CookieBrowserSessionService) and does NOT check AuthenticationType against one expected scheme
// (unlike AgentCallerContext) — see ICurrentPrincipalAccessor's Design region (round-1 review M1) for
// the precise merged-identity claim-resolution model: a policy accepting EITHER scheme
// (credential-management) makes both of those single-scheme techniques unnecessary because
// HttpContext.User, by the time a handler runs, already carries a ClaimsIdentity for EVERY scheme
// the request successfully authenticated against (ordinarily exactly one), not a single "winning"
// scheme's principal. Reading the claim directly off HttpContext.User is therefore both simpler and
// correct for any scheme the policy admits, including future schemes added to
// CredentialManagementDefaults.Policy's AddAuthenticationSchemes list without this class needing a
// corresponding change; in the unusual both-succeeded case, FindFirstValue's merge-order-dependent
// result is still a principal the caller demonstrably controls — see ICurrentPrincipalAccessor's
// Design region for why that is fail-safe without needing a defined precedence.
// Null-safe throughout (no exceptions): no HttpContext, unauthenticated identity, missing claim, or
// an unparsable/empty guid all return null rather than throwing — mirrors
// CookieBrowserSessionService.GetCurrentPrincipalIdAsync's "corrupted input reads as not
// authenticated, never crashes the request" posture.
#endregion

namespace TimeWarp.Architecture.Services;

using System.Security.Claims;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;

public sealed class HttpCurrentPrincipalAccessor : ICurrentPrincipalAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public HttpCurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken)
  {
    ClaimsPrincipal? user = HttpContextAccessor.HttpContext?.User;
    if (user?.Identity is not { IsAuthenticated: true })
    {
      return Task.FromResult<PrincipalId?>(null);
    }

    string? claimValue = user.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty)
    {
      return Task.FromResult<PrincipalId?>(null);
    }

    return Task.FromResult<PrincipalId?>(PrincipalId.From(guid));
  }
}
