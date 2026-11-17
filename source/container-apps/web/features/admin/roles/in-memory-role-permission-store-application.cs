#region Purpose
// Skip-mode IRolePermissionStore: reads a role's permission ids from the in-memory Role aggregate store.
#endregion

#region Design
// Task 473: the authorization read port stays in the platform cluster (the permission evaluator,
// lockout guards, agent-scope permission seed, and principals slice read it without depending on
// Admin.Roles, TWA0009); the Roles slice implements it over the aggregate. Writes go through
// IRoleStore (SetRolePermissions handler), so there is one owner of the grant set. A role with
// no row reads as empty, as before. Singleton over the singleton InMemoryRoleStore;
// PostgresDbModule replaces it with EfRolePermissionStore when connected.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>Role permission reads over <see cref="InMemoryRoleStore"/>.</summary>
public sealed class InMemoryRolePermissionStore : IRolePermissionStore
{
  private readonly InMemoryRoleStore Roles;

  public InMemoryRolePermissionStore(InMemoryRoleStore roles)
  {
    Roles = roles ?? throw new ArgumentNullException(nameof(roles));
  }

  public async Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    Role? role = await Roles.FindAsync(RoleId.From(roleId), cancellationToken).ConfigureAwait(false);
    return role is null ? [] : [.. role.Permissions.Select(permission => permission.PermissionId)];
  }
}
