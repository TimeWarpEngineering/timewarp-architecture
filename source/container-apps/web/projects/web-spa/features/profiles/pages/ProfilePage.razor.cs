#region Purpose
// Registers the Profile route and authorize policy; markup and behavior live in ProfilePage.razor.
#endregion

#region Design
// Page gate stays PermissionIds.ProfileRead (task 148 D3/D8). Save is a separate PUT
// (UpdateProfile, profile.write) — never a register/session gate. Data loads only once
// interactive (RendererInfo.IsInteractive; server prerender has no WASM HttpClient
// BaseAddress). Fetch and save go through ProfileState (TWA0022). Loading is
// FetchProfileData [TrackAction]. The form edits UpdateProfile.Command, built with
// Command.From(Profile) when the page loads and when Cancel is clicked, not on every
// render. That copy sets WorkingCopyReady. OnAfterRenderAsync leaves a prefill in state
// until the flag is set, so a draft that arrives during the fetch is not applied onto
// the empty command and is not cleared. Save dispatches the command and copies again so
// the form matches the stored response. Layout is FormSection (Profile + Preferences)
// with FormField full-width controls and FormActions.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[Page("/Profile", Policy = PermissionIds.ProfileRead, Navigable = true, Description = "View and replace the signed-in person's profile.", NavOrder = 25, AgentActions = [typeof(ProfileState.UpdateProfileActionSet.Action)])]
[Authorize(Policy = PermissionIds.ProfileRead)]
partial class ProfilePage;
