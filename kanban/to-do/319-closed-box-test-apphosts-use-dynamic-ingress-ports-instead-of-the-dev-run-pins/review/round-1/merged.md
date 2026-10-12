# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:48
- Description: The same-file heuristic in the source scan can miss a boot split across files.
- Suggestion: Accept.
- Source: general
- Disposition notes: Every current boot is single-file, and the model fact independently guards the helper's ports. Decided by the review oracle.

### M2 — Severity: nit — Status: wontfix
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:86
- Description: The default-pins fact depends on ambient config overrides.
- Suggestion: Accept.
- Source: general
- Disposition notes: It intentionally asserts the committed `dev run` defaults, and port overrides do not belong in user secrets. Decided by the review oracle.

### M3 — Severity: suggestion — Status: wontfix
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:72
- Description: The model facts assume an `ingress` resource, and aspire-tests is not yarp-excluded.
- Suggestion: Out of scope.
- Source: general
- Disposition notes: Pre-existing. ingress-smoke-tests.cs has the identical dependency on master, so this is not a regression from this diff. Decided by the review oracle.

## Duplicates / conflicts

- None.
