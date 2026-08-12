#region Purpose
// Admin roles list page: GetRoles table with permission membership multi-select + New Role.
#endregion

#region Design
// Task 147-004 / 182-004: primary Admin/Roles surface; RolePage (/Admin/Roles/New) is secondary.
// Policy PermissionIds.AdminRolesRead matches server GetRoles/GetRole; SetRolePermissions is
// admin.roles.manage (server 403 if the signed-in principal only has read). Inline checkboxes
// edit DraftPermissionIds; Save posts SetRolePermissions (protected-core enforced server-side).
// Loading is FetchRoles [TrackAction] (COPIC IsAnyActive), not Roles is null.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles", Policy = PermissionIds.AdminRolesRead)]
[Authorize(Policy = PermissionIds.AdminRolesRead)]
partial class RolesListPage
{
  private bool IsLoading =>
    IsAnyActive(typeof(RoleState.FetchRolesActionSet.Action));

  protected override async Task OnInitializedAsync() => await RoleState.FetchRoles();
}
