#region Purpose
// Adds ClaimTypes.Role for each effective product role on every authenticated request.
#endregion

#region Design
// Task 147-004 D8: the identity-session cookie stays PrincipalId-only (no roles baked in).
// IClaimsTransformation runs after authentication and projects effective roles so
// RequireRole(Administrator Guid) on admin policies works for passkey sessions without
// re-issuing cookies when assignment changes. Scoped lifetime: depends on scoped
// IEffectiveRolesResolver path (resolver is scoped so it can resolve EfPrincipalRoleStore).
// Only adds claims when PrincipalId claim is present; leaves agent bearer / anonymous alone.
// Role claim values are Guid strings matching RoleIds so SPA and server RequireRole agree.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Identity;

/// <summary>Projects effective roles onto the request principal as ClaimTypes.Role.</summary>
public sealed class PrincipalRoleClaimsTransformation : IClaimsTransformation
{
  private readonly IEffectiveRolesResolver EffectiveRolesResolver;

  public PrincipalRoleClaimsTransformation(IEffectiveRolesResolver effectiveRolesResolver)
  {
    EffectiveRolesResolver = effectiveRolesResolver;
  }

  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    string? claimValue = principal.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty)
    {
      return principal;
    }

    var principalId = PrincipalId.From(guid);
    IReadOnlyList<Guid> roleIds = await EffectiveRolesResolver
      .GetEffectiveRoleIdsAsync(principalId)
      .ConfigureAwait(false);

    if (principal.Identity is not ClaimsIdentity identity)
    {
      return principal;
    }

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
}
