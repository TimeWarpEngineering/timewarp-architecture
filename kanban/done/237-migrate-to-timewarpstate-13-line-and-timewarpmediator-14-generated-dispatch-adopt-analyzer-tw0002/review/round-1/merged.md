# Round 1 — merged findings
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
- Description: Purpose still says the host uses "toast-handler removal" so ExceptionNotification does not need FluentToastProvider. This PR stopped removing the handler (`RemoveAll` is a no-op against the generated concrete Publisher) and instead swallows `FluentServiceProviderException<FluentToastProvider>` in the toast handlers; the end-of-method comments already describe that, but Purpose is wrong. Design still mentions "ExceptionNotification removal" as current semantics (lines 10–12).
- Suggestion: Reword Purpose (and the matching Design sentence) to say headless toast handlers swallow missing FluentToastProvider, not that the handler is removed.
- Source: general
- Disposition notes: Purpose and Design rewritten to describe generated Publisher resolving the concrete ExceptionNotificationHandler and toast handlers swallowing FluentServiceProviderException for headless hosts. Same task id; no sibling apply-findings task.

## Duplicates / conflicts

- None. Single reviewer.
