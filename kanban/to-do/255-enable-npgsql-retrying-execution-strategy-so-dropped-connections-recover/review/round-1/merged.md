# Round 1 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs:108
- Description: replay path clears the whole change tracker of the scoped context.
- Suggestion: narrow to this claim's rows or document safety.
- Source: general
- Disposition notes: wontfix (orchestrator). Every EF store method sharing the scoped context is a
  single `SaveChanges` unit with `AsNoTracking` reads, so no pending (Added/Modified) state outlives
  a store call; the only state a failed attempt can leave is this claim's own rows (Added, or
  accepted after a SaveChanges whose commit failed). Full `ChangeTracker.Clear()` inside the strategy
  is EF's documented replay pattern, the Design region already records it, and narrowing would add
  a key-matching filter with no behavioral gain today.

## Duplicates / conflicts

- none (single reviewer)
