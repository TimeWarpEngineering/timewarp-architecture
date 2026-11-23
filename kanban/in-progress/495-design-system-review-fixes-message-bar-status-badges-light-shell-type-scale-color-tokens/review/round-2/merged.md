# Round 2 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:245
- Description: (round 1) `--twe-danger-tint` was declared only on `:root`, so dark mode kept
  the light pink fill for the error message bar and the error badges.
- Suggestion: re-declare the 12% mix in the dark block.
- Source: general
- Disposition notes: Fixed in `bf6441618`. The dark block declares
  `color-mix(in srgb, var(--twe-danger) 12%, var(--twe-paper))`. I checked the math by hand:
  4.89:1 against `#ef9a9a`, matching the painted fill recorded in Results.
  `dark-danger-tint-tests.cs` requires the declaration in the dark block. The recaptured
  dark message-bar and status-badge shots show the dark fill.

## New findings

None.

## Duplicates / conflicts

- None.
