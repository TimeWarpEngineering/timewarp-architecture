# Round 1 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/features/admin/roles/role-domain.cs:16
- Description: Design region line 16 is 144 characters; the edited paragraph was not reflowed.
- Suggestion: Reflow the Design paragraph to the region's width.
- Source: general
- Disposition notes: wontfix (review oracle). Cosmetic only; no line-length rule is enforced (the
  same file already has code lines over 100, and `dev build` / `ganda repo audit` pass). Not worth
  a fix pass and a second review round; the next edit of this region can reflow it.

## Duplicates / conflicts

- None (single reviewer).
