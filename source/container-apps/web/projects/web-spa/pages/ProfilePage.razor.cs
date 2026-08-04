#region Purpose
// Code-behind for the Profile page: declares the route and gates it behind an authenticated user.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile", Policy = Policies.CanViewOwnProfile)]
[Authorize(Policy = Policies.CanViewOwnProfile)]
partial class ProfilePage;
