#region Purpose
// Registers the Settings route, authorize policy, and CrossSliceReference; markup and behavior live in SettingsPage.razor.
#endregion

#region Design
// Task 167 product Settings UI; task 169 rehomes data through TimeWarp.State (COPIC rule):
// every backend HTTP call is a CredentialsState ActionSet — never page-local List<> + ceremony
// client GetResponse. Loading is Section + FetchCredentials; IsBusy = any tracked
// credentials action. Create/merge sequence FetchCredentials only when CeremonyFailed is
// false — Fetch HandleSuccess resets the flag, so a failed ceremony must not refresh.
// Revoke / Rename sequence FetchCredentials (DefaultApiHandler publishes problem details).
// Credentials is null remains the fetch-once guard (no snapshot yet).
// Task 247: this page renders NO outcome bars. Ceremony success/failure and revoke outcomes
// are published by the CredentialsState handlers to NotificationState and painted once by
// the shell's MessageBars region (TWA0025). Backend surface remains 104-005 (GetCredentials,
// AddPasskey, RevokeCredential).
// RFC 219 D10: "Link Microsoft 365" runs CredentialsState.LinkMicrosoft365, whose handler does the
// full navigation to the BFF challenge (mode=link) — the same action the Ctrl-K palette runs.
// The Microsoft 365 section is gated on SignInState.Microsoft365Offered (FetchMicrosoft365Offered,
// the same GetEntraSignInOffered flag Login reads). The page injects no NavigationManager or API
// service: every interaction here is a dispatch.
// Task 225: site Entra policy moved to Admin/Authentication; this page keeps passkeys + link.
// Task 229: hide Link when an active EntraAccount exists; disable Unlink when it is the last
// active credential (hint: Add a passkey first). Both are the server's flags (task 282:
// GetCredentials.Response.CanLinkMicrosoft365, CredentialSummary.CanRevoke), not client
// predicates. FetchCredentials runs during prerender so first HTML matches them.
// Task 250: the card has no subtitle; the Entra row's title is its Label (the provider,
// "Microsoft 365") and its context line is the linked account (AccountHint).
// Task 230: "Add an existing passkey" next to Create runs the merge ceremony
// (CredentialsState.AddExistingPasskey). 229 card rules apply to the merged credential set.
// Task 233: markup is Section + CredentialList + FluentButton (Primary / Outline / danger Outline).
// No page-local twe-settings vocabulary; no raw <button>.
// Task 246: the passkey row action is Revoke (not Delete) and is disabled with a visible hint
// when the row's CanRevoke flag is false (the last active credential of any kind).
// Task 282 (server-owned availability): Revoke / Unlink, Rename and Link Microsoft 365 show from
// the server's typed flags (CredentialSummary.CanRevoke / CanRename, Response.CanLinkMicrosoft365,
// all from Identity's CredentialRules) — this page computes no rule. The buttons dispatch the real
// actions directly with the row's item (RevokeCredential(id), RenameCredential(id, nickname)), then
// FetchCredentials so the list and its flags refresh; the endpoint still refuses a stale action
// (409). Ctrl-K adds nothing on this page: item actions live here, not in the palette.
// Task 248-001: CredentialList rows carry nickname/context/fingerprint and an inline Rename that
// dispatches CredentialsState.RenameCredential then FetchCredentials; the passkey list is handed
// PendingListRenameCredentialId (null while AddPasskeyPrompt owns the pending nickname, so the
// page never shows two editors) so a just-created passkey opens its editor prefilled with the
// provider name; Cancel there clears the pending state (ClearPendingNickname). Revoke/Unlink is
// two-step inside CredentialList (restating confirmation) and both steps honor 246's disable.
// Rename outcomes land in the shell notification region, not the page-local bars below.
// Task 253: "Signed in · TimeWarp account · <fingerprint>" heads the page so the account matches
// its password-manager entries (every new passkey's WebAuthn user name). The fingerprint is the
// AccountFingerprint claim the identity-session auth state projects from GetCurrentSession (or the
// hosted provider derives during prerender); the text is PasskeyAccountName, the same formatter
// StartPasskeyRegistration uses. Mock auth has no such claim, so the line is simply absent.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[Page("/Settings", Policy = PermissionIds.SettingsRead, Navigable = true, Description = "Manage passkeys and linked accounts for the signed-in person.")]
[Authorize(Policy = PermissionIds.SettingsRead)]
[CrossSliceReference(typeof(CredentialsState), "Settings is Applications chrome; credentials list/create/revoke live on Identity CredentialsState.")]
[CrossSliceReference(typeof(CredentialList), "Settings composes the Identity credential list; Applications owns the page chrome.")]
[CrossSliceReference(typeof(SiteSettingsState), "Settings reads site settings for the passkey prompt.")]
[CrossSliceReference(typeof(PasskeyAccountName), "Settings shows the signed-in account name with the same Identity formatter that names new passkeys.")]
[CrossSliceReference(typeof(SignInState), "Settings shows the Microsoft 365 section when the server offers sign-in, same flag as Login.")]
partial class SettingsPage;
