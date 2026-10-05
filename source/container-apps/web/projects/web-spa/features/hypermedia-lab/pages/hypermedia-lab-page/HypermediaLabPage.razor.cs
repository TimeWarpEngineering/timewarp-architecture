#region Purpose
// Registers the hypermedia lab route and authorize policy; markup and behavior live in HypermediaLabPage.razor.
#endregion

#region Design
// Task 275 evaluation page — removed (or replaced by the adopted approach on the real credentials
// pages) once Steve decides; it must not ship in a template release as-is. Gated on
// credential.manage.self, the permission every signed-in human holds and the policy the lab
// endpoints enforce, so the page, its nav link, its Ctrl-K page row and its server reads agree.
// Route is the literal the [Page] attribute carries; HypermediaLabContextSource matches the current
// path against it, and a web-spa test pins it to the generated PageRegistry entry.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

[Page("/HypermediaLab", Policy = PermissionIds.CredentialManageSelf, Navigable = true)]
[Authorize(Policy = PermissionIds.CredentialManageSelf)]
partial class HypermediaLabPage
{
  public const string Route = "/HypermediaLab";
}
