#region Purpose
// EF-mapped row: one permission id granted to one product role (durable role→permission grants).
#endregion

#region Design
// Task 182-001: host-owned join row, mirror of PrincipalRoleAssignment. Composite key
// (RoleId, PermissionId); no navigation properties. Logical link only — RoleIds are compile-time
// Guids, not a roles table row. Features substrate namespace (same as IRolePermissionStore) so
// Identity + Admin + evaluator stay free of TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>One stored permission grant for a product role (EF row for role_permissions).</summary>
public sealed class RolePermissionGrant
{
  public Guid RoleId { get; set; }

  public string PermissionId { get; set; } = string.Empty;
}
