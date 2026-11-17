#region Purpose
// Test-only helpers that rewrite a role's grants through IRoleStore, the way the roles handlers do.
#endregion

#region Design
// Task 473 retired the Set methods on IRolePermissionStore / IRoleAgentScopeStore: the Role
// aggregate owns both sets. Integration tests that strip or restore a grant around a request
// load the Role, call the full-replace setter, and save, so the server's own read ports see it.
// Namespace is the Authorization global using, so call sites need no new using.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Architecture.Features.Admin.Roles.Application;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;

internal static class RoleGrantTestExtensions
{
  public static async Task SetPermissionIdsForRoleAsync(this IRoleStore roleStore, Guid roleId, IReadOnlyList<string> permissionIds)
  {
    Role role = await roleStore.FindAsync(RoleId.From(roleId)) ?? throw new InvalidOperationException($"No role {roleId}.");
    role.SetPermissions(permissionIds);
    await roleStore.UpdateAsync(role);
  }

  public static async Task SetScopesForRoleAsync(this IRoleStore roleStore, Guid roleId, IReadOnlyList<string> scopes)
  {
    Role role = await roleStore.FindAsync(RoleId.From(roleId)) ?? throw new InvalidOperationException($"No role {roleId}.");
    role.SetAgentScopes(scopes);
    await roleStore.UpdateAsync(role);
  }
}
