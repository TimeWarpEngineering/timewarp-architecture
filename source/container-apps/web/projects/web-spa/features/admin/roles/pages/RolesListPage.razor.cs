#region Purpose
// Registers the Admin/Roles route and authorize policy; markup and behavior live in RolesListPage.razor.
#endregion

#region Design
// Task 206: primary Admin/Roles surface is a summary list (name, description, count/chips).
// RoleDetailPage (/Admin/Roles/{RoleId}) owns membership; RolePage (/Admin/Roles/New) is create.
// Policy PermissionIds.AdminRolesRead matches server GetRoles/GetRole; SetRolePermissions is
// admin.roles.manage (detail Save 403 if the signed-in principal only has read).
// Loading is FetchRoles [TrackAction] (COPIC IsAnyActive), not Roles is null.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles", Policy = PermissionIds.AdminRolesRead)]
[Authorize(Policy = PermissionIds.AdminRolesRead)]
partial class RolesListPage;
