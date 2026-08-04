#region Purpose
// Code-behind for the admin New Role page, which demos contract-interface form binding (IRoleDetails): [Page] drives source-generated routing.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles/New", Policy = Policies.CanViewRolesPage)]
[Authorize(Policy = Policies.CanViewRolesPage)]
partial class RolePage;
