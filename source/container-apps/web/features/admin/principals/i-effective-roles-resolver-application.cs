#region Purpose
// Single source of truth for a principal's effective product roles.
#endregion

#region Design
// Task 147-004 effective-role algorithm, with the task 466 kind exception (SSOT):
//   stored = IPrincipalRoleStore.GetRoleIds(principalId)
//   when stored is empty:
//     principal = IPrincipalStore.GetPrincipal(principalId)
//     effective = {} when principal.Kind is Agent or Service
//     effective = { Member } when the row is missing or Kind is Human
//   when stored is non-empty: effective = HashSet(stored)
//   if bootstrap contains principalId: effective += Administrator, Member (any kind)
//   return ordered by RoleIds.All
// A self-registered agent (complete-agent-key-registration, Kind Agent, no human credential)
// therefore has no role until an admin assigns one. Member stays the human default, so a
// roleless human who holds an agent key still receives Member's agent scopes. Service is not
// a human and gets no default. A missing principal row is not an agent registration, so it
// keeps Member. Used by GetCurrentSession, IClaimsTransformation, ListPrincipals, and
// agent-scope approval so SPA claims, server RequireRole, the admin UI, and issuance never
// disagree. Features substrate namespace (not Admin.Principals slice) so Identity can resolve
// roles without TWA0009.
// Task 160: a role-store or principal-store read failure throws RoleResolutionFailedException
// (503 via RoleResolutionFailureMiddleware). Do not treat an unreadable store as empty →
// Member; that would 403 and hide the outage, and it would hand Member to an agent.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Resolves effective product role Guids for a principal.</summary>
public interface IEffectiveRolesResolver
{
  /// <summary>Stored roles plus the human Member default and bootstrap union, ordered by <see cref="RoleIds.All"/>.</summary>
  /// <exception cref="RoleResolutionFailedException">Thrown when the role store or the principal store cannot be read.</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
  Task<IReadOnlyList<Guid>> GetEffectiveRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
