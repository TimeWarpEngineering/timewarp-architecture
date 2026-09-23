#region Purpose
// Registers the Passkeys route and authorize policy; markup and behavior live in PasskeysPage.razor.
#endregion

#region Design
// Product human CTA lives on /Login (task 104-016). This page remains a discoverable technical
// demo under Nav → Pages so operators can exercise the raw ceremony without the product copy.
// Ceremony mapping is shared via PasskeyCeremonyClient — do not reintroduce Passwordless.dev or
// direct passwordless.* JS interop here.
// Mock mode: ceremony contracts have no GetMockResponseFactory; mock chain yields 501 and the
// page reports it with NotificationState.ReportProblem — the shell region paints it; this page
// renders no outcome bars (task 247, TWA0025).
// RP-ID credential scoping (task 104-031): register and authenticate on the SAME host.
// Task 233: the credential list is the shared CredentialList (same as Settings). This page
// is still the Developer ceremony playground — not a product Settings replacement.
// Task 246: Revoke is disabled with a visible hint when the row is the last active credential
// of ANY kind (CredentialsState.ActiveCredentialCount counts passkeys + agent keys + Entra,
// the same set RevokeCredential.Handler counts via ListCredentialsAsync(includeRevoked: false)).
// The client mirrors the server so the button is never offered when it can only 409; the
// server LastCredential guard remains the authority. State-driven: Revoke → Fetch refreshes
// the count, so the last remaining row flips to disabled without a reload.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

// Technical ceremony demo — product CTA is /Login. Nav + route gated to Developer (147-001).
[Page("/Passkeys", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class PasskeysPage;
