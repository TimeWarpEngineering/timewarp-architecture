# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/roving-selection.cs:329
- Description: The root cancel script swallowed modified Arrow, Home, and End chords.
- Source: general (round 1)
- Disposition notes: `CancelScript` now cancels only when Alt, Ctrl, Meta, and Shift are all up, which matches `OnKeyDownAsync`. Both render suites pin the rendered guard. Fixed in 2d4892b38.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/roving-selection.cs:271
- Description: The cancelled-key list and `FromKey` agreed only by comment.
- Source: general (round 1)
- Disposition notes: `HandledKeys` is the single list. The script is generated from it, `FromKey` gates on it, and a test walks every key through both. Fixed in 2d4892b38.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/roving-value-group.cs:371
- Description: The `@code` blocks and test parsers were duplicated across the two controls.
- Source: general (round 1)
- Disposition notes: The abstract `RovingValueGroup` base and the shared `RovingMarkup` test helper replace the copies. Fixed in 2d4892b38.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/roving-value-group.cs:436
- Description: The PascalCase method-scope variable `MoveResult` shadowed the type.
- Source: general (round 1)
- Disposition notes: Renamed to `move`. Fixed in 2d4892b38.

## New findings

None.

## Duplicates / conflicts

- None this round.
