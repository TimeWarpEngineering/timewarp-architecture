#region Purpose
// Code-behind for the admin role detail page: [Page] route with RoleId and read policy.
#endregion

#region Design
// Task 206: membership editor lives here, not on the list. Policy is AdminRolesRead so a
// read-only principal can view the bundle; Save and checkbox writes require AdminRolesManage
// (honest disable in markup). RoleId is generated onto the partial from the route token.
// Protected-core UI lock uses PermissionIds.IsProtectedCoreLocked; server SetRolePermissions
// still 409s a strip. Last-admin is SetPrincipalRoles, not this page.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles/{RoleId:Guid}", Policy = PermissionIds.AdminRolesRead)]
[Authorize(Policy = PermissionIds.AdminRolesRead)]
partial class RoleDetailPage;
