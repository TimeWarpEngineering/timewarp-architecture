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
// Task 160: store read failures throw RoleResolutionFailedException (503 via
// RoleResolutionFailureMiddleware). Do not treat an unreadable store as empty → Member; that
// would 403 and hide the outage.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Resolves effective product role Guids for a principal.</summary>
public interface IEffectiveRolesResolver
{
  /// <summary>Stored roles plus bootstrap/Member defaults, ordered by <see cref="RoleIds.All"/>.</summary>
  /// <exception cref="RoleResolutionFailedException">Thrown when the role store cannot be read.</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
  Task<IReadOnlyList<Guid>> GetEffectiveRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
