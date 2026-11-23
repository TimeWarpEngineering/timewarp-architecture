#region Purpose
// Code-behind for the admin New Role page: create form beside an edit form.
#endregion

#region Design
// Task 206 kept this route create-only. Task 489-003 puts RoleEditForm beside RoleForm so
// create and edit share RoleDetailsValidator on one page. The edit column loads the
// Administrator role through GetRole. RoleForm still navigates to RoleDetailPage only when
// LastCreatedRoleId changes. The detail page edits whichever role the route names.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page
(
  "/Admin/Roles/New",
  Policy = PermissionIds.AdminRolesManage,
  AgentActions =
  [
    typeof(RoleState.CreateRoleActionSet.Action),
    typeof(RoleState.UpdateRoleActionSet.Action)
  ]
)]
[Authorize(Policy = PermissionIds.AdminRolesManage)]
partial class RolePage;
