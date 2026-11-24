#region Purpose
// Code-behind for the admin New Role page (name + description). Membership is RoleDetailPage.
#endregion

#region Design
// Task 206: this route is create-only. Task 507 removed the Administrator edit column that
// 489-003 added: a page titled New Role does not edit an existing role. RoleEditForm lives on
// RoleDetailPage (/Admin/Roles/{RoleId}), which also carries the UpdateRole agent action.
// RoleForm navigates there only when LastCreatedRoleId changes.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles/New", Policy = PermissionIds.AdminRolesManage, AgentActions = [typeof(RoleState.CreateRoleActionSet.Action)])]
[Authorize(Policy = PermissionIds.AdminRolesManage)]
partial class RolePage;
