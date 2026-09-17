#region Purpose
// Registers the Admin/Principals route and authorize policy; markup and behavior live in PrincipalsPage.razor.
#endregion

#region Design
// Task 147-004 D9 / 182-003: list + inline roles, no detail route. Policy = admin.principals.read.
// Loading is Section + FetchPrincipals [TrackAction] (COPIC IsAnyActive), not Principals is null.
// Save sequences SetPrincipalRoles then FetchPrincipals (handlers do not chain a fetch).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals", Policy = PermissionIds.AdminPrincipalsRead)]
[Authorize(Policy = PermissionIds.AdminPrincipalsRead)]
partial class PrincipalsPage;
