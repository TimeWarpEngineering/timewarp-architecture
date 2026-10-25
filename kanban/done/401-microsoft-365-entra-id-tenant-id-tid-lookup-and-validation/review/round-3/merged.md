# Round 3 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### Resolved prior

- M1 (bug): fixed in `81ad53928` (round 2).
- M2 (suggestion): fixed in `81ad53928` (round 2).
- M3 (suggestion): fixed in `81ad53928` (round 2), narrowed by the M6 fix.
- M4 (nit): fixed in `81ad53928` (round 2).
- M5 (nit): fixed in `81ad53928` (round 2).

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:71
- Description: The M3 early return also caught a draft row with a null domain, so a lookup could
  not fill in the domain of a GUID-only or migrated row.
- Source: general (round 2)
- Disposition notes: Fixed in `7cdc03fe7`. The early return requires `entry.Domain is not null`. A
  GUID-only row is replaced by `{id, domain}` through `TryNormalize`, which prefers a non-null domain
  over a null one. `Fill_The_Domain_Of_A_Guid_Only_Row` pins it. `ResolveEntraTenant_Action_Should_`
  passed 4/4 on re-run. The Design region, `auth.md`, and `overview.md` were updated.

No new findings on the fix delta.

## Duplicates / conflicts

- None. Single reviewer.
