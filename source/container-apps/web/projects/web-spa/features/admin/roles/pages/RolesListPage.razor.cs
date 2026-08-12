#region Purpose
// Admin roles list page: table of GetRoles plus navigation to New Role.
#endregion

#region Design
// Task 147-004: primary Admin/Roles surface; RolePage (/Admin/Roles/New) is secondary from here.
// Policy CanViewRolesPage (Administrator) matches server role CRUD APIs.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page("/Admin/Roles", Policy = Policies.CanViewRolesPage)]
[Authorize(Policy = Policies.CanViewRolesPage)]
partial class RolesListPage
{
  protected override async Task OnInitializedAsync() => await RoleState.FetchRoles();
}
