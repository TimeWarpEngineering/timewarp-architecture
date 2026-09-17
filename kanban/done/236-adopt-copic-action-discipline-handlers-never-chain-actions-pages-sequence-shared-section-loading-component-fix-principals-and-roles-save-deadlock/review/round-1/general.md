# Round 1 — general
**Date:** 2026-09-17
**Scope reviewed:** branch task/236-adopt-copic-action-discipline-handlers-never-chain vs origin/master (6ff6d040)

## Summary

The change correctly breaks the Principals/Roles/credentials same-state deadlock: `*-state` Handlers no longer `await XState.`, pages sequence mutation then Fetch (Counter sequences `ResetStore` then `ChangeRoute`), and `ApiHandler` releases the per-state semaphore before `HandleApiResponseAsync`. Shared `Section` (+ isolation CSS, `FluentSpinner`, Policy/Description/Tip/HeaderContent, Attributes splat) replaces the nine hand-written Loading blocks; skill rules and style-guide card are present; source-scan and runtime nested-dispatch probes pass locally. Dominant residual risk is ceremony failure UX: unconditional `FetchCredentials` after AddPasskey/AddExistingPasskey clears `CeremonyError` while the toast path was also removed, so Settings can silently lose the error.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:54-64
- Description: `CreatePasskeyAsync` / `AddExistingPasskeyAsync` always `await FetchCredentials()` after the ceremony action. On API or WebAuthn failure, `AddPasskey`/`AddExistingPasskey` set `CredentialsState.CeremonyError` (and no longer toast — `Fail` dropped `ToastNotificationState.AddProblemDetails`), then a successful Fetch runs `HandleSuccess` which does `CredentialsState.CeremonyError = null` (`credentials-state.fetch-credentials.cs:57`). Settings binds `ErrorMessage => CeremonyError`, so the user sees neither toast nor inline error. Same pattern on `AddPasskeyPrompt.razor:38-41` (no CeremonyError UI there either — previously the toast was the only feedback).
- Suggestion: Sequence Fetch only on success (e.g. when `CeremonyError` is still null / status indicates success), or stop clearing `CeremonyError` in Fetch `HandleSuccess` and restore a failure toast (page- or handler-side) so API/JS failures remain visible. Mirror the fix for AddPasskeyPrompt.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:12-16
- Description: `OnSave` always Fetchs after `SetPrincipalRoles`. `ApiHandler`/`DefaultApiHandler` complete without throwing on API problem details (toast only), so a failed Set (e.g. 409 last-admin) still re-fetches and re-seeds drafts from the server — discarding the user's unchecked/checked edits that previously survived because Fetch lived only in `HandleSuccess`. Same shape on `RoleDetailPage.razor:35-38`.
- Suggestion: Fetch only when the write succeeded (return/status flag from the action, or check that drafts still need refresh), matching the old HandleSuccess-gated refresh.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/handler-nested-dispatch-guard-tests.cs:95-126
- Description: The empty-allow-list scan only flags `await [A-Za-z]+State.` inside `class Handler`. Nested dispatch via `ISender`/`Sender.Send` (as the runtime probe itself uses) is a false negative. Product code is partly covered by TWA0022, but the guard text claims handlers never dispatch another action.
- Suggestion: Extend the scan to `Sender.Send` / `ISender` awaits inside Handler bodies, or narrow the guard message to the `await XState.` pattern and keep relying on TWA0022 + the runtime ApiHandler probe for the semaphore contract.
- Status: open
