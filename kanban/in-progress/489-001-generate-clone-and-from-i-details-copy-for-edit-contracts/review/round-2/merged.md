# Round 2 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/analyzers/details-copy-contracts-tests.cs (deleted)
- Description: The acceptance checks were in a runfile that CI never ran.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec. The checks are now on `get-profile-tests.cs`,
  `update-site-settings-tests.cs`, and `create-role-tests.cs`, and the `web-jaribu-tests` glob
  `features/**/*-tests.cs` compiles all three. The stray runfile is deleted, and
  `### How to validate` is updated.

### M2 — Severity: bug — Status: fixed
- File: source/analyzers/shared/details-copy-classification.cs:103
- Description: An explicit `ICloneable.Clone() => this` got past TWA0047 and the generator.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec. `CloneMethods` resolves the explicit implementation,
  and the analyzer and the generator both use it. Analyzer and generator tests cover the
  explicit form and are recorded as passing.

### M3 — Severity: suggestion — Status: fixed
- File: source/analyzers/shared/details-copy-classification.cs
- Description: The details-copy predicates were copied in three places.
- Source: general (round 1)
- Disposition notes: Fixed in 2f96e5bec. The file is linked into the convention analyzers and
  foundation-contracts-generators, and the private copies are removed.

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:8-17, :136
- Description: The GetRole Clone check (`GetRoleResponse_Given_`) is in the CreateRole runfile,
  but the file's Purpose region and leading comment still say it covers CreateRole only. Nothing in
  the file says why the check is not in a `get-role` runfile. That stale Purpose breaks the
  context-regions maintenance rule (TWA0004 / `tw-agent-context-regions`). It also invites a later
  move into a new `get-role-tests.cs`, which would add the `CA1707` suppression this fix pass
  avoided.
- Suggestion: Update the Purpose region and leading comment to name the GetRole Clone check. Add
  a one-line Design note on why it lives here: a new runfile's `CA1707` `NoWarn` would be above
  the per-file `.timewarp/suppression-baseline.json`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
