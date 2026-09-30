# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/sign-in-state-tests.cs:36
- Description: The checklist says each converted action is dispatched headless, but CreateAccountWithPasskey, UseExistingAccountForMicrosoft365, FetchSession and the FetchMicrosoft365Choice failure path had no fact.
- Suggestion: Add the failure-path and fetch facts, or narrow the checklist wording.
- Source: general
- Disposition notes: Added 4 facts: CreateAccountWithPasskey failed ceremony, UseExistingAccountForMicrosoft365 expired failure, FetchSession true/false/problem→null, and FetchMicrosoft365Choice valid→true / throw→false. SignInActions_Should_ now passes 11/11, and the web-spa-integration-tests suite passes 124/124. Some success paths need a browser authenticator: passkey "/"→/Settings, LastRegistered*, the passkey unsafe-return collapse and the IdentitySession notification. The RouteState conversions are one-line component dispatches of the existing RouteState.ChangeRoute action, which notification-state-tests already exercises. No new fact covers them. The task checklist wording is narrowed to match what is tested.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:54
- Description: The stale SiteSettingsState CrossSliceReference reason names GetEntraSignInOffered.
- Suggestion: Trim the reason.
- Source: general
- Disposition notes: The reason now reads "Settings reads site settings for the passkey prompt."

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor:15
- Description: The credential list is now disabled during ceremonies (IsListBusy covers the tracked ceremony actions). The Design region does not record this.
- Suggestion: Record it in the Design region.
- Source: general
- Disposition notes: Kept the behavior (rename/revoke cannot race a register). Recorded it in the PasskeysPage.razor.cs Design region.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/sign-in-state/sign-in-state.fetch-microsoft-365-choice.cs:22
- Description: Microsoft365ChoiceValid lives in store-global state and is not reset before a fetch, so a revisit paints the previous verdict.
- Suggestion: Reset to null at handler start.
- Source: general
- Disposition notes: The handler now sets Microsoft365ChoiceValid = null before the call. The Design region is updated.

### M5 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:119
- Description: The palette row reads "Link microsoft 365" because of the generic sentence-case rule.
- Suggestion: Optional display-name override hook.
- Source: general
- Disposition notes: The existing catalog-wide display-name rule produces this casing, and the reviewer marked it non-blocking. A per-action display-name override is a catalog feature beyond this task's scope. Decided by: review oracle.

## Duplicates / conflicts

- None (single reviewer).
