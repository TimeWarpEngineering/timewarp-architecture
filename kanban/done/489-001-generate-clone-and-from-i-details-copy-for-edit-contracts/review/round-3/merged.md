# Round 3 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/analyzers/details-copy-contracts-tests.cs (deleted)
- Description: The acceptance checks were in a runfile that CI never ran.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec (verified round 2).

### M2 — Severity: bug — Status: fixed
- File: source/analyzers/shared/details-copy-classification.cs:103
- Description: An explicit `ICloneable.Clone() => this` got past TWA0047 and the generator.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec (verified round 2).

### M3 — Severity: suggestion — Status: fixed
- File: source/analyzers/shared/details-copy-classification.cs
- Description: The details-copy predicates were copied in three places.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec (verified round 2).

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:12-23
- Description: The Purpose region and leading comment did not mention the GetRole Clone check
  or why it lives in this runfile.
- Source: general (round 2)
- Disposition notes: Fixed in 1c1327444. The leading comment and Purpose name
  `GetRole.Response.Clone`. The Design region says a new `get-role-tests.cs` would add a `CA1707`
  `NoWarn` above the per-file suppression baseline. Verified in round 3.

## Duplicates / conflicts

- None. Single reviewer, no new findings.
