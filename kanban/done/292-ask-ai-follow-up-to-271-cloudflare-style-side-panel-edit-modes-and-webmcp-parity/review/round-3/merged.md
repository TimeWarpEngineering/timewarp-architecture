# Round 3 — merged findings
**Date:** 2026-10-10
**Sources:** general (review of merge e09074295 conflict resolution and 5cb3a6023)

## Counts (final, M1–M16 and N1–N3 carried; R1–R2 new)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 9 | 0 |
| nit | 0 | 7 | 0 |

## Resolved prior

M1–M16 and N1–N3 are still fixed. The general reviewer found nothing from either side lost in the merge.

## Issues

### R1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor (SignInAsync)
- Description: No test ran the Ask "Sign in" button the merge rewrote (close the panel, then route to Login).
- Suggestion: Add a Playwright step that clicks `AgentAskSignInButton`.
- Source: general
- Disposition notes: Fixed. `ask-sign-in-playwright-tests.cs` now signs in from Ask's own button: it clicks `AgentAskSignInButton`, waits for `/Login`, checks that `[data-qa=AgentAsk]` is hidden, and then creates the passkey. The no-key test still uses Home "Sign in", so both paths are covered. The click happens on Home, so the return path collapses to plain `/Login`. `LoginPage.GetLoginUrl` owns the return-path encoding.

### R2 — Severity: nit — Status: fixed
- File: AgentAsk.razor:38
- Description: `AgentAsk.ModalId` had no references left after the merge.
- Suggestion: Remove it.
- Source: general
- Disposition notes: Fixed. Removed.

## Duplicates / conflicts

- None.
