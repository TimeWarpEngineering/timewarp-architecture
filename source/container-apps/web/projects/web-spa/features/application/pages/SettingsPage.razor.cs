#region Purpose
// Registers the Settings route, authorize policy, and CrossSliceReference; markup and behavior live in SettingsPage.razor.
#endregion

#region Design
// Task 167 product Settings UI; task 169 rehomes data through TimeWarp.State (COPIC rule):
// every backend HTTP call is a CredentialsState ActionSet — never page-local List<> + ceremony
// client GetResponse. Loading is Section + FetchCredentials; IsBusy = any tracked
// credentials action. Create/merge sequence FetchCredentials only when CeremonyError is
// still null — Fetch HandleSuccess clears CeremonyError, so a failed ceremony must not
// refresh. Revoke still sequences Fetch (DefaultApiHandler toasts on problem details).
// Credentials is null remains the fetch-once guard (no snapshot yet).
// Ceremony Fail no longer toasts from the handler (empty allow-list); Settings binds
// ErrorMessage to CeremonyError. Backend surface remains 104-005 (GetCredentials, AddPasskey,
// RevokeCredential).
// RFC 219 D10: "Link Microsoft 365" is a full navigation to the BFF challenge (mode=link).
// Task 225: site Entra policy moved to Admin/Authentication; this page keeps passkeys + link.
// Task 229: hide Link when an active EntraAccount exists; disable Unlink when it is the last
// active credential (hint: Add a passkey first); card title is Credential.Label, subtitle is
// "Microsoft 365". FetchCredentials runs during prerender so first HTML matches those rules.
// Task 230: "Add an existing passkey" next to Create runs the merge ceremony
// (CredentialsState.AddExistingPasskey). 229 card rules apply to the merged credential set.
// Task 233: markup is Section + CredentialList + FluentButton (Primary / Outline / danger Outline).
// No page-local twe-settings vocabulary; no raw <button>.
// Task 248-001: CredentialList rows carry nickname/context/fingerprint and an inline Rename that
// dispatches CredentialsState.RenameCredential then FetchCredentials; the passkey list is handed
// PendingListRenameCredentialId (null while AddPasskeyPrompt owns the pending nickname, so the
// page never shows two editors) so a just-created passkey opens its editor prefilled with the
// provider name; Cancel there clears the pending state (ClearPendingNickname). Rename outcomes
// land in the shell notification region, not the page-local bars below.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[Page("/Settings", Policy = PermissionIds.SettingsRead)]
[Authorize(Policy = PermissionIds.SettingsRead)]
[CrossSliceReference(typeof(CredentialsState), "Settings is Applications chrome; credentials list/create/revoke live on Identity CredentialsState.")]
[CrossSliceReference(typeof(CredentialList), "Settings composes the Identity credential list; Applications owns the page chrome.")]
[CrossSliceReference(typeof(ChallengeEntra), "Settings link CTA navigates to the Identity BFF Entra challenge; no WASM MSAL.")]
[CrossSliceReference(typeof(SiteSettingsState), "Settings reads site settings for passkey prompt; Microsoft 365 section is gated on GetEntraSignInOffered.")]
[CrossSliceReference(typeof(GetEntraSignInOffered), "Settings shows the Microsoft 365 section when the server offers sign-in, same flag as Login.")]
partial class SettingsPage;
