#region Purpose
// GetRole: stores one role response for the edit form and the detail page.
#endregion

#region Design
// Generated from [ContractState]. OnGetRoleLoaded assigns the response object. Nothing on the
// state copies its fields into other properties. A fetch for a different id clears the previous
// snapshot before the request returns, so the edit form does not keep showing the other role.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

partial class RoleState
{
  partial void OnBuildingGetRoleRequest(GetRole.Query request)
  {
    if (RoleSnapshot?.RoleId != request.RoleId)
    {
      RoleSnapshot = null;
    }
  }

  private partial void OnGetRoleLoaded(GetRole.Response response)
  {
    RoleSnapshot = response;
  }
}
