# Round 2 — merged findings
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
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:54-73; source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:40-67
- Description: Ceremony failure no longer wiped by an unconditional post-mutation Fetch; Settings helper and AddPasskeyPrompt gate on `CeremonyError is not null`, and AddPasskeyPrompt renders the error bar.
- Suggestion: Sequence Fetch only on success; surface CeremonyError on AddPasskeyPrompt.
- Source: general
- Disposition notes: Re-verified in round 2. FetchCredentials only when CeremonyError is still null; AddPasskeyPrompt FluentMessageBar `data-qa=AddPasskeyPromptError`.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:12-20; source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:35-43
- Description: Pages Fetch only when the Set HandleSuccess flag is true, so DefaultApiHandler problem-details failures keep drafts.
- Suggestion: Fetch only when the write succeeded.
- Source: general
- Disposition notes: Re-verified in round 2. `LastSetPrincipalRolesSucceeded` / `LastSetRolePermissionsSucceeded` false in GetRequest, true in HandleSuccess.

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/handler-nested-dispatch-guard-tests.cs:92-148
- Description: Empty-allow-list Handler scan now also yields `Sender.Send`; assertion message covers `await XState.Y or Sender.Send`.
- Suggestion: Extend the scan to `Sender.Send` inside Handler bodies.
- Source: general
- Disposition notes: Re-verified in round 2.

## Duplicates / conflicts

- Carried stable M1–M3 IDs from round 1. No new findings.
