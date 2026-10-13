#region Purpose
// Registers the Admin/Principals/{id} route and authorize policy; markup and behavior live in PrincipalDetailPage.razor.
#endregion

#region Design
// Task 328. Not navigable: the Principals list is the entry. admin.principals.read opens the
// page; the profile card sits inside an AuthorizeView on admin.profiles.read (the server also
// returns a null profile without it), and the role editor inside one on admin.principals.manage
// (SetPrincipalRoles enforces the same policy). This page is the single role-editing surface.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

[Page("/Admin/Principals/{PrincipalId:Guid}", Policy = PermissionIds.AdminPrincipalsRead)]
[Authorize(Policy = PermissionIds.AdminPrincipalsRead)]
partial class PrincipalDetailPage;
