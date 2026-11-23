#region Purpose
// SetRoleAgentScopes: success flag and snapshot refresh for the generated save action.
#endregion

#region Design
// Same shape as SetRolePermissions. The page owns the agent-scope working copy and passes
// Scopes on the command. Success does not fetch. The open role's scope list becomes the
// echoed scopes. A failed write leaves LastSetRoleAgentScopesSucceeded false.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRoleAgentScopes;

partial class RoleState
{
  partial void OnBuildingSetRoleAgentScopesRequest(Command request)
  {
    _ = request;
    LastSetRoleAgentScopesSucceeded = false;
  }

  partial void OnSetRoleAgentScopesSucceeded(Response response)
  {
    LastSetRoleAgentScopesSucceeded = true;
    if (RoleSnapshot is not { } snapshot)
    {
      return;
    }

    RoleSnapshot = new GetRole.Response
    (
      roleId: snapshot.RoleId,
      name: snapshot.Name,
      description: snapshot.Description,
      permissions: [.. snapshot.Permissions],
      agentScopes: [.. response.Scopes]
    );
  }
}
