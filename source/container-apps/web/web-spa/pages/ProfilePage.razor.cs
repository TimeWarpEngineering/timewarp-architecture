#region Purpose
// Code-behind for the Profile page: declares the route and gates it behind an authenticated user.
#endregion

namespace TimeWarp.Architecture.Pages;

[Page("/Profile")]
[Authorize]
partial class ProfilePage;
