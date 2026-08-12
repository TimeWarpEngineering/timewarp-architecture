#region Purpose
// EF-mapped row: one product role Guid assigned to one principal (durable principal→role grants).
#endregion

#region Design
// Task 147-006: host-owned join row, not on TimeWarp.Identity Principal. Composite key
// (PrincipalId, RoleId); no navigation properties. Logical link to identity.principals (no FK)
// so migrations and store replace-set stay simple; orphaned role rows if a principal is
// deleted elsewhere are acceptable until a delete cascade story exists.
// Features substrate namespace — same as IPrincipalRoleStore (Identity + Admin without TWA0009).
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>One stored role assignment for a principal (EF row for principal_roles).</summary>
public sealed class PrincipalRoleAssignment
{
  public PrincipalId PrincipalId { get; set; }

  public Guid RoleId { get; set; }
}
