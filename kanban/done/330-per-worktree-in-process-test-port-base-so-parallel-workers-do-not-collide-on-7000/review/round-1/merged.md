# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: tests/common/timewarp-testing/in-proc-test-ports.cs:14
- Description: Design region claimed the derived range avoids the ephemeral range; slots 32000+ overlap Linux 32768–60999.
- Suggestion: Document the overlap and rationale.
- Source: general
- Disposition notes: Design region now states the overlap and why 13 slots are kept.

### M2 — Severity: nit — Status: fixed
- File: tests/common/timewarp-testing/in-proc-test-ports.cs:150
- Description: Override Describe() text implied dev test exports the derived base even for an explicit override.
- Suggestion: Reword.
- Source: general
- Disposition notes: Now "checkout X would derive Y; `dev test` sets the variable for its children". in-proc-test-ports-tests 11/11, `dev build` green.

## Duplicates / conflicts

- None.
