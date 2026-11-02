#region Purpose
// EF-mapped row: one agent scope granted directly on a principal.
#endregion

#region Design
// Task 466: host-owned join row, mirror of PrincipalRoleAssignment. Composite key
// (PrincipalId, Scope). No navigation properties and no FK, so a deleted principal can leave
// orphaned rows until a delete-cascade story exists. Platform cluster so Identity and Admin
// stay free of TWA0009.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Identity;

/// <summary>One stored agent scope for a principal (EF row for principal_agent_scopes).</summary>
public sealed class PrincipalAgentScopeGrant
{
  public PrincipalId PrincipalId { get; set; }

  public string Scope { get; set; } = string.Empty;
}
