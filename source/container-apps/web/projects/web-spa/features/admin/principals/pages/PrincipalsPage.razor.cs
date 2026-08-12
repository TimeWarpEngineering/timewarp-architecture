#region Purpose
// Admin principals page: list principals with inline multi-select role assignment.
#endregion

#region Design
// Task 147-004 D9 / 182-003: list + inline roles, no detail route. Policy = admin.principals.read.
// Loading is FetchPrincipals [TrackAction] (COPIC IsAnyActive), not Principals is null.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals", Policy = PermissionIds.AdminPrincipalsRead)]
[Authorize(Policy = PermissionIds.AdminPrincipalsRead)]
partial class PrincipalsPage
{
  private bool IsLoading =>
    IsAnyActive(typeof(PrincipalState.FetchPrincipalsActionSet.Action));

  protected override async Task OnInitializedAsync() => await PrincipalState.FetchPrincipals();
}
