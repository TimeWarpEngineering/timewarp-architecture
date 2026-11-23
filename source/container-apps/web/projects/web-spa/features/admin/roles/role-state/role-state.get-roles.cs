#region Purpose
// GetRoles: stores the role list. Membership drafts are not part of this snapshot.
#endregion

#region Design
// The GetRolesActionSet (Action, DefaultApiHandler, and RoleState.GetRoles(...)) is generated from
// [ContractState] on RoleState (task 483). This file holds the one hook the generator requires for a fetch:
// OnGetRolesLoaded. It stores the rows for the list page. Permission and agent-scope working copies
// live on RoleDetailPage, seeded from GetRole, so this hook does not copy them.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

partial class RoleState
{
  private partial void OnGetRolesLoaded(GetRoles.Response response)
  {
    RoleRows = [.. response.Items];
  }
}
