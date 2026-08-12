#region Purpose
// SPA state for admin roles: list from GetRoles, permission drafts, last-created id.
#endregion

#region Design
// Roles null = not loaded (loading UI); empty array = loaded with zero rows.
// LastCreatedRoleId remains for create-round-trip demos after RoleForm submit.
// DraftPermissionIds (task 182-004): multi-select edits per role before Save → SetRolePermissions;
// seeded from GetRoles.RoleDto.PermissionIds on fetch (same pattern as PrincipalState drafts).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

[StateAccess]
public sealed partial class RoleState : State<RoleState>
{
  private List<RoleDto>? RolesList { get; set; }
  private Dictionary<Guid, HashSet<string>> DraftPermissionIds { get; set; } = new();

  public IReadOnlyList<RoleDto>? Roles => RolesList?.AsReadOnly();

  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public override void Initialize()
  {
    RolesList = null;
    LastCreatedRoleId = null;
    DraftPermissionIds = new();
  }

  public IReadOnlyCollection<string> GetDraftPermissionIds(Guid roleId) =>
    DraftPermissionIds.TryGetValue(roleId, out HashSet<string>? set)
      ? set
      : Array.Empty<string>();

  public bool IsPermissionSelected(Guid roleId, string permissionId) =>
    DraftPermissionIds.TryGetValue(roleId, out HashSet<string>? set)
    && set.Contains(permissionId);
}
