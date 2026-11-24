#region Purpose
// PermissionTree groups for the role detail page: permissions by dotted prefix, agent scopes
// by resource, and the protected-core ids that are locked for a role's current draft.
#endregion

#region Design
// Task 512. Permission groups come from PermissionIds.GroupsByPrefix (derived from All, not
// a second catalog); a group key is the prefix and its label is prefix.*. Agent scopes come
// from AgentScopes.All, grouped by the resource before the colon in first-seen order, label
// resource:*. Locked is every selected id that PermissionIds.IsProtectedCoreLocked holds for
// the role, so a selected Administrator admin.* grant is disabled and a missing core stays
// enabled for repair. The server still rejects a strip (409). This type decides the lock;
// PermissionTree only shows it.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

internal static class RoleMembershipTrees
{
  internal static IReadOnlyList<PermissionTreeGroup> Permissions { get; } =
  [
    .. PermissionIds.GroupsByPrefix.Select(
      static group => new PermissionTreeGroup(group.Prefix, group.Prefix + ".*", group.PermissionIds))
  ];

  internal static IReadOnlyList<PermissionTreeGroup> AgentScopeGroups { get; } =
  [
    .. TimeWarp.Identity.AgentScopes.All
      .GroupBy(ScopeResource, StringComparer.Ordinal)
      .Select(static group => new PermissionTreeGroup(group.Key, group.Key + ":*", [.. group]))
  ];

  internal static IReadOnlySet<string> LockedPermissions(Guid roleId, IReadOnlySet<string> selected)
  {
    ArgumentNullException.ThrowIfNull(selected);
    HashSet<string> locked = new(StringComparer.Ordinal);
    foreach (string permissionId in selected)
    {
      if (PermissionIds.IsProtectedCoreLocked(roleId, permissionId, isSelected: true))
      {
        locked.Add(permissionId);
      }
    }

    return locked;
  }

  private static string ScopeResource(string scope)
  {
    int colon = scope.IndexOf(':', StringComparison.Ordinal);
    return colon < 0 ? scope : scope[..colon];
  }
}
