#region Purpose
// SPA state for admin roles: the role list, the open role, and the last created id.
#endregion

#region Design
// RoleList null = no snapshot yet; empty array = loaded with zero rows. It is not named Roles: CA1721
// reads Roles beside the generated GetRoles method as confusing. RoleSnapshot is the same choice
// for one role: a property named Role beside the generated GetRole method is CA1721.
// [ContractState] generates GetRoles, GetRole, SetRolePermissions, and SetRoleAgentScopes.
// CreateRole and UpdateRole stay hand-written: each is an agent tool whose action carries the
// command (State 12.0.0-beta.19 cannot catalog a generated action as an agent tool with a typed
// parameter). GetRole stores the response object. UpdateRole replaces its name and description
// and keeps the permission and agent-scope lists. Permission and agent-scope drafts are not
// store fields. The detail page owns those sets and sends the commands it built.
// In-flight fetch is [TrackAction] on the generated GetRoles and GetRole actions.
// LastCreatedRoleId remains for the create form (navigates to RoleDetailPage when it changes).
// LastSetRolePermissionsSucceeded and LastSetRoleAgentScopesSucceeded are the page-side gates
// so a failed write does not replace the page's working sets.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

[StateAccess]
[ContractState
(
  typeof(GetRoles),
  typeof(GetRole),
  typeof(SetRolePermissions),
  typeof(SetRoleAgentScopes)
)]
public sealed partial class RoleState : State<RoleState>
{
  private List<RoleDto>? RoleRows { get; set; }

  public IReadOnlyList<RoleDto>? RoleList => RoleRows?.AsReadOnly();

  public GetRole.Response? RoleSnapshot { get; private set; }

  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public bool LastSetRolePermissionsSucceeded { get; private set; }
  public bool LastSetRoleAgentScopesSucceeded { get; private set; }

  public override void Initialize()
  {
    RoleRows = null;
    RoleSnapshot = null;
    LastCreatedRoleId = null;
    LastSetRolePermissionsSucceeded = false;
    LastSetRoleAgentScopesSucceeded = false;
  }
}
