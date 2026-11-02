#region Purpose
// EF-mapped row: one agent scope granted to one product role.
#endregion

#region Design
// Task 466: host-owned join row, mirror of RolePermissionGrant. Composite key (RoleId, Scope).
// No navigation properties. Logical link only — role ids are not a roles table row.
// Platform cluster so Identity and Admin stay free of TWA0009.
#endregion

namespace TimeWarp.Architecture.Authorization;

/// <summary>One stored agent scope for a product role (EF row for role_agent_scopes).</summary>
public sealed class RoleAgentScopeGrant
{
  public Guid RoleId { get; set; }

  public string Scope { get; set; } = string.Empty;
}
