#region Purpose
// Code-behind for the Profile page: declares the route and gates it behind an authenticated user.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile")]
[Authorize]
partial class ProfilePage;
