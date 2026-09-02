#region Purpose
// Code-behind for the admin New Role page (name + description). Membership is RoleDetailPage.
#endregion

#region Design
// Task 206: this route stays create-only. Permission membership is RoleDetailPage
// (/Admin/Roles/{RoleId}); RoleForm navigates there only when LastCreatedRoleId changes.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles/New", Policy = PermissionIds.AdminRolesManage)]
[Authorize(Policy = PermissionIds.AdminRolesManage)]
partial class RolePage;
