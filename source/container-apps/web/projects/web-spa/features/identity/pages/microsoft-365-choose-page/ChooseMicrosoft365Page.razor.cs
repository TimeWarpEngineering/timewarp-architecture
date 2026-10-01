#region Purpose
// Registers /Login/Microsoft365/Choose; markup and behavior live in ChooseMicrosoft365Page.razor.
#endregion

#region Design
// Anonymous focused chrome (same as Login). Parked Entra claims are server-side; this page
// only peeks validity (SignInState.FetchMicrosoft365Choice) and dispatches create vs already-have
// (CreateAccountFromMicrosoft365 / UseExistingAccountForMicrosoft365). The handlers notify
// IdentitySessionAuthenticationStateProvider before navigating; already-have reuses
// StartPasskeyAuthentication then CompleteEntraBootstrapExisting so the Entra attach and session
// happen together. "Sign in again" is a RouteState.ChangeRoute to Login. The page injects no
// NavigationManager, API service or ceremony client.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[Page("/Login/Microsoft365/Choose")]
partial class ChooseMicrosoft365Page;
