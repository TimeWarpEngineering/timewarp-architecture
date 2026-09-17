# Round 2 — general
**Date:** 2026-09-17
**Scope reviewed:** post-fix delta for M1–M3 plus branch vs origin/master

## Summary

Re-verified the uncommitted M1–M3 fixes against the working tree and round-1 disposition. M1 stays fixed: Settings and AddPasskeyPrompt FetchCredentials only when `CeremonyError` is still null, AddPasskey/AddExistingPasskey clear then set `CeremonyError` on Fail/JSException (no toast), and AddPasskeyPrompt surfaces the error via `FluentMessageBar` (`data-qa=AddPasskeyPromptError`). M2 stays fixed: `LastSetPrincipalRolesSucceeded` / `LastSetRolePermissionsSucceeded` are cleared in GetRequest and set true only in HandleSuccess, and both OnSave paths skip Fetch on false so a 409 no longer re-seeds drafts. M3 stays fixed: the Handler source scan also flags `Sender.Send` and the assertion names both patterns. No new defects found in the fix delta.

## Issues

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:54-73; source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:40-67
- Description: Ceremony failure no longer wiped by an unconditional post-mutation Fetch; Settings helper and AddPasskeyPrompt gate on `CeremonyError is not null`, and AddPasskeyPrompt renders the error bar.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:12-20; source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:35-43; matching `LastSet*` flags on PrincipalState / RoleState Set handlers
- Description: Pages Fetch only when the Set HandleSuccess flag is true, so DefaultApiHandler problem-details failures keep drafts.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/handler-nested-dispatch-guard-tests.cs:92-148
- Description: Empty-allow-list Handler scan now also yields `Sender.Send`; assertion message covers `await XState.Y or Sender.Send`.
- Status: fixed
