# Round 4 — merged findings
**Date:** 2026-11-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Source: general (round 1)
- Disposition notes: fixed in round 2 (e06c648f7, 52efa0805).

### M2 — Severity: suggestion — Status: fixed
- Source: general (round 1)
- Disposition notes: fixed in round 2.

### M3 — Severity: suggestion — Status: fixed
- Source: general (round 2)
- Disposition notes: fixed in 925b61eb3 (round 3).

### M4 — Severity: nit — Status: fixed
- Source: general (round 2)
- Disposition notes: fixed in 925b61eb3 (round 3).

## Duplicates / conflicts

- None. Round 4 reviewed the publish-bundle fix (15fef86b0, Oakton `PrivateAssets=all`) and raised no
  new findings. The one concern checked, whether `--api false` consumers lose `Oakton.dll` at runtime,
  was refuted with a scratch build: ResolveAssemblyReference still copies Oakton and its dependencies as
  `reference` entries. See `general.md`.
