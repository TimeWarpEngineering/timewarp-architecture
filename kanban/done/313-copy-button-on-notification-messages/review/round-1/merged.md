# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.copy-message.cs:73
- Description: Only `JSException` is caught. An `OperationCanceledException` from a JS-interop
  timeout or a disposed host escapes the handler and turns into a pipeline Error bar ("A task
  was canceled."). That breaks the "never throws" rule.
- Suggestion: When the caller's token is live, an OCE is a failed copy (Warning). When the
  token is cancelled, swallow it. Add a test.
- Source: general
- Disposition notes: Fixed on this task. The handler's exception filter now catches
  `JSException`, plus an OCE while the token is live, and reports the Warning. A cancelled
  token is swallowed. The Design region now explains this. New test:
  `Report_Warning_When_Clipboard_Times_Out`.

## Duplicates / conflicts

- None (single reviewer).
