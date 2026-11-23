#region Purpose
// SetRolePermissions: success flag and snapshot refresh for the generated save action.
#endregion

#region Design
// The page owns the permission working copy and passes PermissionIds on the command.
// The action is generated from [ContractState] and is not a draft request, so the handler
// copies those ids. OnBuilding clears the success flag before the send. Success stores the
// echoed ids on the open role and sets the flag. A failed write leaves the snapshot and the
// flag alone, so the page keeps its working copy. This file has no toggle action.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRolePermissions;

partial class RoleState
{
  partial void OnBuildingSetRolePermissionsRequest(Command request)
  {
    _ = request;
    LastSetRolePermissionsSucceeded = false;
  }

  partial void OnSetRolePermissionsSucceeded(Response response)
  {
    LastSetRolePermissionsSucceeded = true;
    if (RoleSnapshot is not { } snapshot)
    {
      return;
    }

    RoleSnapshot = new GetRole.Response
    (
      roleId: snapshot.RoleId,
      name: snapshot.Name,
      description: snapshot.Description,
      permissions: [.. response.PermissionIds],
      agentScopes: [.. snapshot.AgentScopes]
    );
  }
}
