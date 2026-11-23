#region Purpose
// SPA state for admin roles: list from GetRoles, permission drafts, last-created id.
#endregion

#region Design
// RoleList null = no snapshot yet; empty array = loaded with zero rows. It is not named Roles: CA1721
// reads Roles beside the generated GetRoles method as confusing.
// [ContractState] (task 483) generates the GetRoles, SetRolePermissions and SetRoleAgentScopes action sets,
// their handlers and RoleState.GetRoles() / SetRolePermissions(Command) / SetRoleAgentScopes(Command). The hooks
// are in the matching partial files. CreateRole stays hand-written: it is the Role.CreateRole agent tool, and a
// generated action's catalog entry cannot be an agent tool with a typed parameter (State 12.0.0-beta.19).
// In-flight fetch is [TrackAction] on the generated GetRoles action — pages use IsAnyActive, not null.
// LastCreatedRoleId remains for create-round-trip demos after RoleForm submit (navigates to
// RoleDetailPage when set).
// DraftPermissionIds (task 182-004 / 206): multi-select edits on RoleDetailPage before Save →
// SetRolePermissions; seeded from GetRoles.RoleDto.Permissions on fetch (same pattern as
// PrincipalState drafts). The list page is summary-only and does not mutate drafts.
// LastSetRolePermissionsSucceeded is the page-side Fetch gate (failed 409 must not re-seed).
// DraftAgentScopes (task 466) is the separate agent-scope list on the same page.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

[StateAccess]
[ContractState
(
  typeof(GetRoles),
  typeof(SetRolePermissions),
  typeof(SetRoleAgentScopes),
  DraftRequests = [typeof(SetRolePermissions), typeof(SetRoleAgentScopes)]
)]
public sealed partial class RoleState : State<RoleState>
{
  private List<RoleDto>? RoleRows { get; set; }
  private Dictionary<Guid, HashSet<string>> DraftPermissionIds { get; set; } = [];
  private Dictionary<Guid, HashSet<string>> DraftAgentScopes { get; set; } = [];

  public IReadOnlyList<RoleDto>? RoleList => RoleRows?.AsReadOnly();

  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public bool LastSetRolePermissionsSucceeded { get; private set; }
  public bool LastSetRoleAgentScopesSucceeded { get; private set; }

  public override void Initialize()
  {
    RoleRows = null;
    LastCreatedRoleId = null;
    DraftPermissionIds = [];
    DraftAgentScopes = [];
    LastSetRolePermissionsSucceeded = false;
    LastSetRoleAgentScopesSucceeded = false;
  }

  public IReadOnlyCollection<string> GetDraftPermissionIds(Guid roleId) =>
    DraftPermissionIds.TryGetValue(roleId, out HashSet<string>? set)
      ? set
      : Array.Empty<string>();

  public bool IsPermissionSelected(Guid roleId, string permissionId) =>
    DraftPermissionIds.TryGetValue(roleId, out HashSet<string>? set)
    && set.Contains(permissionId);

  public IReadOnlyCollection<string> GetDraftAgentScopes(Guid roleId) =>
    DraftAgentScopes.TryGetValue(roleId, out HashSet<string>? set)
      ? set
      : Array.Empty<string>();

  public bool IsAgentScopeSelected(Guid roleId, string scope) =>
    DraftAgentScopes.TryGetValue(roleId, out HashSet<string>? set)
    && set.Contains(scope);
}
