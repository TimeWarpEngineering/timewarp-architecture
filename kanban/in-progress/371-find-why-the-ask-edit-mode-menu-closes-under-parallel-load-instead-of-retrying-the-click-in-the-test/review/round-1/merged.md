# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:36
- Description: The document-wide `MutationObserver` (`watchTooltips` / `markHint`) duplicates the
  `showPopover` prototype wrapper. The popover type only matters at show time, and every FluentUI
  tooltip show already goes through the wrapper. The observer adds a permanent subtree observer to
  every generated app and does nothing for correctness.
- Suggestion: Keep only the `showPopover` wrapper. Or name the path the wrapper misses in the Design
  region and cover it in the test.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:91
- Description: `queueMicrotask(wrapWhenDefined)` is a guaranteed no-op, because `tweHintWrapped` is
  already set on the prototype. The Design region's claim that the wrap happens "after the library's
  own wrap" is not guaranteed, and the order does not matter.
- Suggestion: Remove the microtask re-call. Reword the Design region to say that the wrapper composes
  with FluentUI's `isConnected` override in either order.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
