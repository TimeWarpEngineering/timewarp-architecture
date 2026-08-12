#region Purpose
// Single source of truth for a principal's effective product roles.
#endregion

#region Design
// Task 147-004 effective-role algorithm (SSOT):
//   stored = IPrincipalRoleStore.GetRoleIds(principalId)
//   effective = stored.Count == 0 ? { Member } : HashSet(stored)
//   if bootstrap contains principalId: effective += Administrator, Member
//   return ordered by RoleIds.All
// Used by GetCurrentSession, IClaimsTransformation, and ListPrincipals so SPA claims, server
// RequireRole, and admin UI never disagree. Features substrate namespace (not Admin.Principals
// slice) so Identity can resolve roles without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Resolves effective product role Guids for a principal.</summary>
public interface IEffectiveRolesResolver
{
  Task<IReadOnlyList<Guid>> GetEffectiveRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
