# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/settings/site-settings-boot-pending-migrations-tests.cs:66
- Description: A fixed 3s delay before asserting the wait log makes the regression timing-dependent on host build speed.
- Suggestion: Poll for the wait log with a deadline, then confirm the boot stays blocked.
- Source: general
- Disposition notes: Fixed on this task. The test now polls up to 30s for the log naming the latest migration, waits 2s more, asserts the boot has not completed, then asserts the log is present. `dev build` 0/0; web-server-integration-tests 341/341, with the wait log seen at attempts 1–3.

## Duplicates / conflicts

- None (single reviewer).
