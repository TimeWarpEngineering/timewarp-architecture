# Round 1 — merged findings
**Date:** 2026-09-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:54-64
- Description: `CreatePasskeyAsync` / `AddExistingPasskeyAsync` always `await FetchCredentials()` after the ceremony action. On API or WebAuthn failure, `AddPasskey`/`AddExistingPasskey` set `CredentialsState.CeremonyError` (and no longer toast — `Fail` dropped `ToastNotificationState.AddProblemDetails`), then a successful Fetch runs `HandleSuccess` which does `CredentialsState.CeremonyError = null` (`credentials-state.fetch-credentials.cs:57`). Settings binds `ErrorMessage => CeremonyError`, so the user sees neither toast nor inline error. Same pattern on `AddPasskeyPrompt.razor:38-41` (no CeremonyError UI there either — previously the toast was the only feedback).
- Suggestion: Sequence Fetch only on success (e.g. when `CeremonyError` is still null / status indicates success), or stop clearing `CeremonyError` in Fetch `HandleSuccess` and restore a failure toast (page- or handler-side) so API/JS failures remain visible. Mirror the fix for AddPasskeyPrompt.
- Source: general
- Disposition notes: FetchCredentials only when CeremonyError is still null (Settings helper + AddPasskeyPrompt). AddPasskeyPrompt shows CeremonyError in a FluentMessageBar (`data-qa=AddPasskeyPromptError`).

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:12-16
- Description: `OnSave` always Fetchs after `SetPrincipalRoles`. `ApiHandler`/`DefaultApiHandler` complete without throwing on API problem details (toast only), so a failed Set (e.g. 409 last-admin) still re-fetches and re-seeds drafts from the server — discarding the user's unchecked/checked edits that previously survived because Fetch lived only in `HandleSuccess`. Same shape on `RoleDetailPage.razor:35-38`.
- Suggestion: Fetch only when the write succeeded (return/status flag from the action, or check that drafts still need refresh), matching the old HandleSuccess-gated refresh.
- Source: general
- Disposition notes: `LastSetPrincipalRolesSucceeded` / `LastSetRolePermissionsSucceeded` false in GetRequest, true in HandleSuccess. Pages Fetch only when the flag is true.

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/handler-nested-dispatch-guard-tests.cs:95-126
- Description: The empty-allow-list scan only flags `await [A-Za-z]+State.` inside `class Handler`. Nested dispatch via `ISender`/`Sender.Send` (as the runtime probe itself uses) is a false negative. Product code is partly covered by TWA0022, but the guard text claims handlers never dispatch another action.
- Suggestion: Extend the scan to `Sender.Send` / `ISender` awaits inside Handler bodies, or narrow the guard message to the `await XState.` pattern and keep relying on TWA0022 + the runtime ApiHandler probe for the semaphore contract.
- Source: general
- Disposition notes: Scan also flags `Sender.Send` inside Handler bodies; assertion message names both patterns.

## Duplicates / conflicts

- None (single reviewer).
