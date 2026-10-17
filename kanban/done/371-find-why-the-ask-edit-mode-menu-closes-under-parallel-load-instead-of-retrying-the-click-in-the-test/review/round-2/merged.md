# Round 2 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts
- Description: The document `MutationObserver` duplicated the `showPopover` wrapper.
- Verification: `watchTooltips` and `markHint` are gone. Only the prototype wrapper is left. Every
  FluentUI 5.0.0 tooltip show goes through `this.showPopover()`.
- Source: general (round 1)

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts
- Description: The `queueMicrotask(wrapWhenDefined)` re-wrap did nothing, and the Design region claimed an
  install order.
- Verification: The microtask is gone. The Design region now says the wrapper composes with FluentUI's
  `isConnected` override in either order. This matches the bundle, where both overrides sit on the
  prototype and call the function they captured.
- Source: general (round 1)

## Issues

None new.

## Duplicates / conflicts

- None (single reviewer).
