# Round 2 — merged findings
**Date:** 2026-09-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs:4
- Description: Purpose still said the host uses "toast-handler removal"; generated Publisher resolves ExceptionNotificationHandler by concrete type so RemoveAll is a no-op.
- Suggestion: Reword Purpose and Design to describe the FluentServiceProviderException swallow.
- Source: general (round 1); re-verified round 2
- Disposition notes: Purpose and Design rewritten. Round 2 confirmed they match ConfigureServices comments and both toast handlers.

## Resolved prior

- M1 (nit): fixed. Re-verified against Purpose/Design, ConfigureServices trailing comments, `toast-notification-state.exception-notification-handler.cs`, and `toast-notification-state.problem-details-notification-handler.cs`.

## Duplicates / conflicts

- None. Single reviewer. No new findings on the fix delta.
