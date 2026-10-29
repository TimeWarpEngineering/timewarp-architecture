#region Purpose
// Registers the route for a developer diagnostic page that displays the signed-in user's
// AuthenticationState claims; markup and behavior live in ClaimsPage.razor.
#endregion

namespace TimeWarp.Architecture.Features.Developer;

[Page("/Developer/Claims", Policy = PermissionIds.DeveloperClaimsRead, Navigable = true, Description = "Developer view of the signed-in person's AuthenticationState claims.", NavGroup = "Developer", NavOrder = 340)]
[Authorize(Policy = PermissionIds.DeveloperClaimsRead)]
partial class ClaimsPage;
