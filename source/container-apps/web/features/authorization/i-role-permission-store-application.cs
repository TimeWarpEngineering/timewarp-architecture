#region Purpose
// Port for durable role → permission membership (roles as mutable permission bundles).
#endregion

#region Design
// Task 182-001: dual-mode mirror of IPrincipalRoleStore — InMemoryRolePermissionStore singleton
// default (seeded from RolePermissionSeed); EfRolePermissionStore scoped when Postgres is connected
// (PostgresDbModule). Get returns empty when a role has no rows (not an error). Set replaces the
// full permission set for a role (empty clears). Features substrate namespace so evaluator,
// Identity, and Admin can resolve grants without TWA0009. Protected-core / last-admin guards
// live in SetRolePermissions / SetPrincipalRoles handlers (182-004) — this port stays a dumb store.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Durable assignment of permission ids to a product role (web-app concern).</summary>
public interface IRolePermissionStore
{
  /// <summary>Stored permission ids for the role — empty when nothing has been granted.</summary>
  Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default);

  /// <summary>Replace stored permissions for the role (empty clears all grants for that role).</summary>
  Task SetPermissionIdsForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> permissionIds,
    CancellationToken cancellationToken = default);
}
