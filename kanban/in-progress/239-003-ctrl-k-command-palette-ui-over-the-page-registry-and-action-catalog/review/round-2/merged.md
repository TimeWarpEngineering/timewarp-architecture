# Round 2 — merged findings (re-verification of round-1 fixes)
**Date:** 2026-09-30
**Sources:** general (orchestrator re-verification against the fix delta)
**Scope:** fix delta on `CommandPalette.razor(.css)`, `command-palette-js-module.cs`, `command-palette.ts`.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 2 | 0 |

## Resolved prior

- **M1 fixed** — `open()` only captures when `returnFocusTo` is null or detached; a repeat Ctrl-K keeps the original target.
- **M2 fixed** — `Disposed` set first in `Dispose`; a handle returned after disposal is disposed immediately; cancellation/disconnect after disposal is swallowed (filter is `Disposed && …`, so live-page failures still surface); TS `.catch` on `invokeMethodAsync`.
- **M3 fixed** — `ScrollIntoView(id)` export, invoked when `HighlightedIndex` changes while active (`ScrolledIndex` resets on close).
- **M4 fixed** — `@onmousedown:preventDefault` on list, empty text and hint; row `@onclick` still runs.
- **M5 fixed** — Enter re-filters when `CommandPaletteState.Query != QueryText`.
- **M6 fixed** — `aria-expanded`/`aria-controls` bound to `Matches.Count > 0`; visually hidden `<h2 id="modal-title">`; ArrowUp/Down caret suppression via `InputAttribute` selector in the document keydown.
- **M7 fixed** — manual browser checklist extended in task.md Results.
- **M8 wontfix** — unchanged (shared ModalContainer focus trap; rationale in round-1 merged.md).

## New findings

None. Regions reconciled (CommandPalette Design block, TS Design region, JS module Design region).

## Gates on the fix

- `dev build`: 0 warnings, 0 errors.
- `web-spa-integration-tests --filter-class CommandPalette`: 23/23 passed.
