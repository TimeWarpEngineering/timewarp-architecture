# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:156
- Description: The deferred `show()` runs while `customElements.whenDefined("fluent-button")`
  is still pending, and nothing cancels it. If `Close()` runs before that resolves, `Close()`
  is a no-op, and `showModal` fires afterwards anyway. The result is an empty modal panel
  that makes the page inert, with no close button, after the caller has closed the drawer.
- Suggestion: keep a `wantOpen` flag. `Show` sets it true, `Close` and `Dispose` set it false,
  and the deferred `show` returns early unless the flag is still true.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor.css:34
- Description: `DrawerSide.End` and `Start` are logical names, but the CSS is physical
  (`right` and `left`, `border-left` and `border-right`, `translateX(±100%)`). Under RTL, End
  renders on the start edge. `Profile.razor.css` already uses logical insets.
- Suggestion: switch to `inset-inline-*` and `border-inline-*`, and flip the slide direction
  under `:dir(rtl)`.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:142
- Description: The module has no `close` listener. A native close it did not start, such as
  `<form method="dialog">` in `ChildContent`, leaves the caller's `IsOpen` and `ActiveModalId`
  set while the dialog is shut.
- Suggestion: add a `close` listener that calls `RequestClose` unless the module itself
  started the close.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:112
- Description: `Show` or `Close` interop runs on every render, not only when the open state
  changes.
- Suggestion: track the last state applied and call JS only on a change.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-10-23). Calling JS on every render
  re-syncs a dialog that drifted from `IsOpen`. Both calls are no-ops when the state already
  matches, and the cost is one async interop call per render of a page with a drawer. That
  trade-off is acceptable for a template leaf.

## Duplicates / conflicts

- None. There was one reviewer.
