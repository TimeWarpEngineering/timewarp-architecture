# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** same as framework (branch vs master, commit 81d32f50c)

## Summary

The change adds `Drawer` as a native modal `<dialog>`. Open state belongs to the caller
(`IsOpen` and `OnClose`). A small TypeScript module calls `showModal` and `close`, forwards
`cancel` and backdrop clicks as `OnClose`, moves the dialog onto `document.body`, and wraps Tab
around the Fluent button shadow host. The style-guide card is wired through
`SetActiveModal`, `ActiveModalId`, and `CloseModal`, as the Decision section asks. All checklist
items are present, and the implementer restatement matches the task, so there is no misread.

Risk is low. The remaining issues are robustness at the edges of the JS module and one
inconsistency between the logical side names and the physical CSS.

Gates were re-run in this worktree:
- `./bin/dev build`: 0 warnings, 0 errors.
- Drawer render tests: 7 of 7 passed.
- Playwright test `StyleGuideDrawer_Given_Wasm`: 1 of 1 passed.
- `./bin/dev template-smoke`: succeeded.
- `ganda repo audit`: passed.
- `git diff master -- source tests | rg -i crunchit`: printed nothing.

I checked whether moving the Blazor-owned dialog onto `body` breaks Blazor's DOM bookkeeping,
using the shipped `blazor.webassembly.js` (11.0.0-rc.2). Insert, remove, and keyed permute all
locate nodes through their real `parentNode`, or through ranges bounded by the original parent.
A relocated dialog is removed correctly, and siblings are not corrupted. There is no finding
there.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:156
- Description: `Show()` defers `showModal` until `fluent-button` is defined, then waits one
  frame. Nothing cancels that deferred `show` if `Close()` runs first. `Close()` is a no-op
  while the dialog is not open yet. The trigger is `IsOpen` going true and then false before
  `customElements.whenDefined("fluent-button")` resolves, for example on first load, or when
  another `SetActiveModal` replaces the drawer id. The deferred `show()` then opens a modal
  dialog after the component has already set `ShowContent = false`. The result is an empty
  panel with no close button, and it makes the whole page inert until Escape.
- Suggestion: keep a `wantOpen` flag in the handle. `Show` sets it true and `Close` sets it
  false. The deferred `show` returns early unless `wantOpen` is still true.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor.css:34
- Description: `DrawerSide.End` and `Start` are logical names, and the Design regions say "end
  edge" and "start edge". The CSS pins the panel with physical properties: `right: 0` and
  `left: auto`, `border-left` and `border-right`, and `translateX(±100%)`. Under `dir="rtl"`,
  `End` renders on the start edge. The same tree already uses logical insets
  (`features/profiles/components/Profile.razor.css` uses `inset-inline-start` and
  `inset-inline-end`).
- Suggestion: switch to `inset-inline-end: 0` and `inset-inline-start: auto`, with
  `border-inline-start` and `border-inline-end`. Flip the slide keyframes under
  `:dir(rtl)`, or translate with a sign taken from a custom property.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:142
- Description: The module listens for `cancel` and backdrop `click`, but not `close`. A
  native close that the module did not start leaves `IsOpen` true while the dialog is shut.
  One example is a `<form method="dialog">` or `formmethod="dialog"` button in
  `ChildContent`. Another is a future `closedby` or `requestClose` path. When that happens,
  `ActiveModalId` stays set to the drawer id, so "one overlay at a time" is wrong until
  something else clears it.
- Suggestion: add a `close` listener that calls `RequestClose` unless the close came from
  `Close()` or `Dispose()`, for example behind a `closing` guard flag.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:112
- Description: `OnAfterRenderAsync` calls JS (`Show` or `Close`) on every render, not only
  when the open state changes. Callers pass lambdas for `OnClose`, so the drawer re-renders
  on every parent render. That costs one interop round trip per closed drawer each time the
  page renders.
- Suggestion: optional. Remember the last state applied (`AppliedOpen`) and call JS only when
  it changes. The current approach also re-syncs a dialog that drifted, so this is a cost
  trade-off, not a defect.
- Status: open
