# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/splatted-class.cs (deleted)
- Description: The `SplattedClass` helper was a second idiom for a caller `class`.
- Suggestion: Use a `Class` parameter as `Card` does.
- Source: general (round 1)
- Disposition notes: Fixed in b59530c9f. Helper deleted; `KpiCard` and `KpiStrip` render
  `@Class` on the root; unmatched attributes splat directly; Design regions updated. Verified
  round 2.

### M2 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/kpi-strip-render-tests.cs:39
- Description: The child-inside-strip check passed trivially.
- Suggestion: Tie the assertion to the strip root.
- Source: general (round 1)
- Disposition notes: Fixed in b59530c9f. The child is asserted inside the slice between the root's
  opening tag and the closing `</div>` that ends the markup. Verified round 2.

## Duplicates / conflicts

- None (single reviewer). No new findings on the fix delta.
