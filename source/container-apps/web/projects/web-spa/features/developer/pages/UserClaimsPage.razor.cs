#region Purpose
// Registers the route for a developer diagnostic page that displays the signed-in user's claims.
#endregion

namespace TimeWarp.Architecture.Features.Developer;

[Page("/Developer/UserClaims", Policy = PermissionIds.DeveloperClaimsRead)]
[Authorize(Policy = PermissionIds.DeveloperClaimsRead)]
partial class UserClaimsPage;
