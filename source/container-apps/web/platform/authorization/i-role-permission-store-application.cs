#region Purpose
// Read port for a role's permission ids (roles as permission bundles), used by authorization.
#endregion

#region Design
// Task 182-001 introduced the port; task 473 made it read-only. The Role aggregate (Admin.Roles)
// owns the permission set and is written through IRoleStore; this port is how the permission
// evaluator, lockout guards, agent-scope permission seed, and the principals slice read it
// without depending on the Roles slice (TWA0009). The Roles slice implements it:
// InMemoryRolePermissionStore over InMemoryRoleStore (skip-mode), EfRolePermissionStore over the
// aggregate's identity.role_permissions owned rows when Postgres is connected (PostgresDbModule).
// Get returns empty when a role has no grants or does not exist (not an error).
#endregion

namespace TimeWarp.Architecture.Authorization;

/// <summary>Reads the permission ids granted to a role (web-app concern).</summary>
public interface IRolePermissionStore
{
  /// <summary>Stored permission ids for the role — empty when nothing has been granted.</summary>
  Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default);
}
