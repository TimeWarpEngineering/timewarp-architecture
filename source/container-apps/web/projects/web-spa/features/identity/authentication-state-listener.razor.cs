#region Purpose
// Holds CrossSliceReference attributes for AuthenticationStateListener; behavior lives in AuthenticationStateListener.razor.
#endregion

#region Design
// Identity sign-in/out is a deliberate multi-slice edge: Authentication owns the listener, Profiles
// owns profile cache, Authorization owns the current-user/roles cache, Credentials owns the
// passkey list cache (task 169). Documented via CrossSliceReference so TWA0009 sees the coupling
// (razor @code alone is not analyzed).
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

[CrossSliceReference(typeof(ProfileState), "Identity pipeline: on sign-in load the profile for the principal.")]
[CrossSliceReference(typeof(AuthorizationState), "Identity pipeline: on sign-out clear authorization/current-user cache with profile.")]
[CrossSliceReference(typeof(CredentialsState), "Identity pipeline: on sign-out clear credential list cache with profile.")]
partial class AuthenticationStateListener;
