#region Purpose
// Registers the Profile route and authorize policy; markup and behavior live in ProfilePage.razor.
#endregion

#region Design
// Task 148 D3/D8: page stays PermissionIds.ProfileRead; data loads only once interactive
// (same RendererInfo.IsInteractive guard as Profile.razor chrome — server prerender has no
// WASM HttpClient BaseAddress). Fetch goes through ProfileState, never HttpClient from the page.
// Loading is FetchProfileData [TrackAction], not Alias is null (a loaded profile can omit alias).
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile", Policy = PermissionIds.ProfileRead)]
[Authorize(Policy = PermissionIds.ProfileRead)]
partial class ProfilePage;
