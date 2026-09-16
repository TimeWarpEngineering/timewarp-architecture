#region Purpose
// Registers the Settings route, authorize policy, and CrossSliceReference; markup and behavior live in SettingsPage.razor.
#endregion

#region Design
// Task 167 product Settings UI; task 169 rehomes data through TimeWarp.State (COPIC rule):
// every backend HTTP call is a CredentialsState ActionSet — never page-local List<> + ceremony
// client GetResponse. Page owns only chrome UX (expanded row). Loading =
// IsAnyActive(FetchCredentials); IsBusy = any tracked credentials action.
// Credentials is null remains the fetch-once guard (no snapshot yet). API failures toast via
// DefaultApiHandler; browser ceremony failures surface as CredentialsState.CeremonyError.
// Backend surface remains 104-005 (GetCredentials, AddPasskey, RevokeCredential).
// RFC 219 D10: "Link Microsoft 365" is a full navigation to the BFF challenge (mode=link).
// Task 225: site Entra policy moved to Admin/Authentication; this page keeps passkeys + link.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[Page("/Settings", Policy = PermissionIds.SettingsRead)]
[Authorize(Policy = PermissionIds.SettingsRead)]
[CrossSliceReference(typeof(CredentialsState), "Settings is Applications chrome; credentials list/create/revoke live on Identity CredentialsState.")]
[CrossSliceReference(typeof(ChallengeEntra), "Settings link CTA navigates to the Identity BFF Entra challenge; no WASM MSAL.")]
[CrossSliceReference(typeof(SiteSettingsState), "Settings reads EntraSignInEnabled to show Link Microsoft 365; policy editing lives on Admin/Authentication.")]
partial class SettingsPage;
