# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:36
- Description: The document-wide `MutationObserver` (`watchTooltips` / `markHint`) duplicates the
  `showPopover` prototype wrapper. The popover type only matters at show time, and every FluentUI
  tooltip show already goes through the wrapper. The observer adds a permanent subtree observer to
  every generated app and does nothing for correctness.
- Suggestion: Keep only the `showPopover` wrapper. Or name the path the wrapper misses in the Design
  region and cover it in the test.
- Source: general
- Disposition notes: Dropped `watchTooltips` and `markHint`. FluentUI 5.0.0 `showTooltip` calls `this.showPopover()`, and the library override calls the captured prototype method when `isConnected`. The wrapper sets `hint` immediately before that call. The Ask surface test still calls `tooltip.showPopover()` and expects `hint` with the menu open.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:91
- Description: `queueMicrotask(wrapWhenDefined)` is a guaranteed no-op, because `tweHintWrapped` is
  already set on the prototype. The Design region's claim that the wrap happens "after the library's
  own wrap" is not guaranteed, and the order does not matter.
- Suggestion: Remove the microtask re-call. Reword the Design region to say that the wrapper composes
  with FluentUI's `isConnected` override in either order.
- Source: general
- Disposition notes: Removed `queueMicrotask(wrapWhenDefined)`. The Design region now says the wrapper composes with FluentUI's `isConnected` override in either order, because each wrapper calls the function it captured.

## Duplicates / conflicts

- None (single reviewer).
