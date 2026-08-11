#region Purpose
// Registers the route for a developer diagnostic page that displays the signed-in user's claims.
#endregion

namespace TimeWarp.Architecture.Features.Developer;

[Page("/Developer/UserClaims", Policy = Policies.CanViewUserClaimsPage)]
[Authorize(Policy = Policies.CanViewUserClaimsPage)]
partial class UserClaimsPage;
