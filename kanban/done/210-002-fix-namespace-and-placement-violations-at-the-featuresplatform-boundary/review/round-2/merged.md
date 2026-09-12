# Round 2 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs:109
- Description: Living snapshot still named `TimeWarp.Architecture.Features.RolePermissionGrant` after the type moved to `TimeWarp.Architecture.Authorization`.
- Suggestion: Retarget the snapshot entity name; leave historical Designers unchanged.
- Source: general (round 1); re-verified round 2
- Disposition notes: Confirmed. Snapshot entity is `TimeWarp.Architecture.Authorization.RolePermissionGrant`. Older `*.Designer.cs` files still use the Features FQN (expected). No new findings on the fix delta.

## Resolved prior

- Round-1 M1 carried forward as fixed; no new IDs.

## Duplicates / conflicts

- None (single reviewer).
