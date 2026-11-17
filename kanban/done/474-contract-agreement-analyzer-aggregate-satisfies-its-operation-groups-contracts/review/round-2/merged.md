# Round 2 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/contract-agreement-analyzer-tests.cs:271
- Description (round 1): the no-root test had no drift, and it used bare `string.Replace`.
- Source: general (round 1)
- Disposition notes: Confirmed fixed in round 2. The test applies the TWA0041 and TWA0042 drift
  through the asserting `Edit` and moves the root out of the slice. The line-60 `Replace` uses `Edit`,
  and the Design region matches.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:2
- Description (round 1): the leftover "alias" wording in the Purpose line and the `ShellUserName`
  parameter.
- Source: general (round 1)
- Disposition notes: Confirmed fixed in round 2. The Purpose line says "display name", and the
  parameter is `displayName`.

## New findings

- None.

## Duplicates / conflicts

- None (single reviewer).
