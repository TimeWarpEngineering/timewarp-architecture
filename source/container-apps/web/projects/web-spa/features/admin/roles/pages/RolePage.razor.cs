#region Purpose
// Code-behind for the admin New Role page, which demos contract-interface form binding (IRoleDetails): [Page] drives source-generated routing.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles/New", Policy = PermissionIds.AdminRolesManage)]
[Authorize(Policy = PermissionIds.AdminRolesManage)]
partial class RolePage;
