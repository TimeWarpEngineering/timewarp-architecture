#region Purpose
// Registers the Profile route and authorize policy; markup and behavior live in ProfilePage.razor.
#endregion

#region Design
// Page gate stays PermissionIds.ProfileRead (task 148 D3/D8). Save is a separate PUT
// (UpdateProfile, profile.write) — never a register/session gate. Data loads only once
// interactive (RendererInfo.IsInteractive; server prerender has no WASM HttpClient
// BaseAddress). Fetch and save go through ProfileState (TWA0022). Loading is
// FetchProfileData [TrackAction], not Alias is null.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile", Policy = PermissionIds.ProfileRead)]
[Authorize(Policy = PermissionIds.ProfileRead)]
partial class ProfilePage;
