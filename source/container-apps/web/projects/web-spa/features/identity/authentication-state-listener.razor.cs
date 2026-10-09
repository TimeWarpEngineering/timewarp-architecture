#region Purpose
// Holds CrossSliceReference attributes for AuthenticationStateListener; behavior lives in AuthenticationStateListener.razor.
#endregion

#region Design
// Identity sign-in/out is a deliberate multi-slice edge: Identity owns the listener and
// credential-list cache, Profiles owns profile cache, Authorization owns the current-user/roles
// cache (task 169). CredentialsState is same-slice (no opt-out). Documented via CrossSliceReference
// so TWA0009 sees the remaining coupling (razor @code alone is not analyzed).
// RFC 219 D8: sign-in also FetchCredentials so the Entra add-passkey soft prompt has a snapshot
// without visiting Settings. Sign-out dispatches ForgetPasskeySoftPromptLater (interactive only)
// so the next principal on the same tab is not suppressed; the sessionStorage write lives in that
// handler — the listener only dispatches (task 265, TWA0026).
// Task 293: startup and every change also dispatch AgentSurfaceState.LoadChatConfiguration, so the
// Ask readiness follows the session instead of keeping the first (possibly signed-out) answer.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[CrossSliceReference(typeof(ProfileState), "Identity pipeline: on sign-in load the profile for the principal.")]
[CrossSliceReference(typeof(AuthorizationState), "Identity pipeline: on sign-out clear authorization/current-user cache with profile.")]
[CrossSliceReference(typeof(SiteSettingsState), "Identity pipeline: on sign-in load site settings so Required passkey mode is known without visiting Settings.")]
[CrossSliceReference(typeof(AgentSurfaceState), "Identity pipeline: on sign-in and sign-out re-probe Ask readiness, which depends on the session.")]
partial class AuthenticationStateListener;
