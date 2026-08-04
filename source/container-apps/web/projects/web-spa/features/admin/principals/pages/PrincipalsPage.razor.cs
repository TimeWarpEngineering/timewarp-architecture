#region Purpose
// Admin principals page: list principals with inline multi-select role assignment.
#endregion

#region Design
// Task 147-004 D9: list + inline roles, no detail route. CanViewPrincipalsPage = Administrator.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals", Policy = Policies.CanViewPrincipalsPage)]
[Authorize(Policy = Policies.CanViewPrincipalsPage)]
partial class PrincipalsPage
{
  protected override async Task OnInitializedAsync() => await PrincipalState.FetchPrincipals();
}
