#region Purpose
// Admin principals page: list principals with inline multi-select role assignment.
#endregion

#region Design
// Task 147-004 D9 / 182-003: list + inline roles, no detail route. Policy = admin.principals.read.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals", Policy = PermissionIds.AdminPrincipalsRead)]
[Authorize(Policy = PermissionIds.AdminPrincipalsRead)]
partial class PrincipalsPage
{
  protected override async Task OnInitializedAsync() => await PrincipalState.FetchPrincipals();
}
