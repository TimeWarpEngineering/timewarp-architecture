#region Purpose
// Adds ClaimTypes.Role for each effective product role on every authenticated request.
#endregion

#region Design
// Task 147-004 D8: the identity-session cookie stays PrincipalId-only (no roles baked in).
// IClaimsTransformation runs after authentication and projects effective roles so diagnostics
// and any residual RequireRole surfaces see effective roles for passkey sessions without
// re-issuing cookies when assignment changes. Scoped lifetime: depends on scoped
// IEffectiveRolesResolver path (resolver is scoped so it can resolve EfPrincipalRoleStore).
// Task 182-006: ONLY identity-session and mock-identity-session expand roles. Agent-token
// principals also carry PrincipalId claims but must NOT receive human role claims — agents
// authorize via scopes → AgentScopePermissionSeed, never product roles. Anonymous / unknown
// schemes are left alone.
// Role claim values are Guid strings matching RoleIds so SPA and server RequireRole agree.
// Task 160: do not catch RoleResolutionFailedException (or any store failure) here. Swallowing
// as no role claims would present an authenticated principal with no grants (403). The throw
// must leave AuthenticateAsync so RoleResolutionFailureMiddleware can map it to 503 — never
// Challenge 401 (PolicyEvaluator: failed authenticate → Challenge) and never empty-roles 403.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Identity;

/// <summary>Projects effective roles onto human-session principals as ClaimTypes.Role.</summary>
public sealed class PrincipalRoleClaimsTransformation : IClaimsTransformation
{
  private readonly IEffectiveRolesResolver EffectiveRolesResolver;

  public PrincipalRoleClaimsTransformation(IEffectiveRolesResolver effectiveRolesResolver)
  {
    EffectiveRolesResolver = effectiveRolesResolver;
  }

  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    if (principal.Identity is not ClaimsIdentity identity)
    {
      return principal;
    }

    if (!IsHumanSessionScheme(identity.AuthenticationType))
    {
      return principal;
    }

    string? claimValue = principal.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty)
    {
      return principal;
    }

    var principalId = PrincipalId.From(guid);
    IReadOnlyList<Guid> roleIds = await EffectiveRolesResolver
      .GetEffectiveRoleIdsAsync(principalId)
      .ConfigureAwait(false);

    // Drop prior role claims so re-entry / multiple transform calls do not accumulate.
    foreach (Claim existing in identity.FindAll(ClaimTypes.Role).ToArray())
    {
      identity.RemoveClaim(existing);
    }

    foreach (Guid roleId in roleIds)
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, roleId.ToString()));
    }

    return principal;
  }

  private static bool IsHumanSessionScheme(string? authenticationType) =>
    authenticationType is AuthenticationSchemeNames.IdentitySession
      or AuthenticationSchemeNames.MockIdentitySession;
}
