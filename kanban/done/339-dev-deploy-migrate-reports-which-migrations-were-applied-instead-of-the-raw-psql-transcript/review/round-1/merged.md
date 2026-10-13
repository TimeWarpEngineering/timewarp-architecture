# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs (BuildBundleReport)
- Description: The ACA failure report hid the migrations that committed before the failing one.
- Suggestion: List `applying[..^1]` as applied, then the failing id.
- Source: general
- Disposition notes: Fixed on this task. The failure branch now lists the applied ids before `Failed while applying <id>`.
  Tests cover the applied-then-failed, failed-on-first and no-output cases. dev-cli-tests 218/0.

## Duplicates / conflicts

- None (single reviewer).
