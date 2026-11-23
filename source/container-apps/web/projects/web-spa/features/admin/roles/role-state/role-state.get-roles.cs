#region Purpose
// GetRoles: stores the role list and seeds the permission and agent-scope drafts from it.
#endregion

#region Design
// The GetRolesActionSet (Action, DefaultApiHandler, and RoleState.GetRoles(...)) is generated from
// [ContractState] on RoleState (task 483). This file holds the one hook the generator requires for a fetch:
// OnGetRolesLoaded. It is required (CS8795 when missing) because the drafts must be seeded from the server.
// Without the seed, RoleDetailPage shows every checkbox unchecked and Save posts an empty list, which
// SetRolePermissions applies as a full replace (464 §4f). DraftAgentScopes is seeded the same way (task 466).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

partial class RoleState
{
  private partial void OnGetRolesLoaded(GetRoles.Response response)
  {
    RoleRows = [.. response.Items];
    DraftPermissionIds = response.Items.ToDictionary(
      static dto => dto.RoleId,
      static dto => new HashSet<string>(dto.Permissions ?? [], StringComparer.Ordinal));
    DraftAgentScopes = response.Items.ToDictionary(
      static dto => dto.RoleId,
      static dto => new HashSet<string>(dto.AgentScopes ?? [], StringComparer.Ordinal));
  }
}
