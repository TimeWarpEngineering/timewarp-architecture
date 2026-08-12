#region Purpose
// Code-behind for the Profile page: route gate + load profile fields on enter.
#endregion

#region Design
// Task 148 D3/D8: page stays PermissionIds.ProfileRead; data loads only once interactive
// (same RendererInfo.IsInteractive guard as Profile.razor chrome — server prerender has no
// WASM HttpClient BaseAddress). Fetch goes through ProfileState, never HttpClient from the page.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile", Policy = PermissionIds.ProfileRead)]
[Authorize(Policy = PermissionIds.ProfileRead)]
partial class ProfilePage
{
  protected override async Task OnInitializedAsync()
  {
    if (!RendererInfo.IsInteractive)
    {
      return;
    }

    await ProfileState.FetchProfileData();
  }
}
