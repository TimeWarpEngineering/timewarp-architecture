# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:173
- Description: a deferred `showModal` could open after `Close`.
- Source: general (round 1)
- Disposition notes: fixed in 93befc533 with the `wantOpen` flag. Re-verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor.css:37
- Description: End and Start used physical CSS.
- Source: general (round 1)
- Disposition notes: fixed in 93befc533 with inline insets and borders and an RTL slide swap.
  Re-verified in round 2.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:170
- Description: there was no `close` listener for a native close the module did not start.
- Source: general (round 1)
- Disposition notes: the listener landed in 93befc533, and a native close calls
  `RequestClose`. Its skip for module closes is broken. M5 tracks that.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:112
- Description: `Show` or `Close` interop runs on every render.
- Source: general (round 1)
- Disposition notes: wontfix (review oracle, round 1). See round-1/merged.md.

### M5 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:52
- Description: `moduleClosing` is reset in `finally` before the queued `close` event fires, so
  every close the module starts also calls `RequestClose`, and the caller's `OnClose` runs a
  second time. Reproduced against the compiled `drawer.js` in Playwright Chromium:
  `Show()` and `Close()` give 0 host calls synchronously and `["RequestClose"]` one tick later.
  On the `ActiveModalId` pattern, a drawer that hands off to another overlay through
  `SetActiveModal(otherId)` dispatches `CloseModal` from the late `close` event and clears the
  new overlay.
- Suggestion: skip in `onDialogClose` when `wantOpen` is already false, with `placeOnBody`
  marked separately, or use a pending-module-close token that the handler takes. Keep the M3
  behaviour for `method="dialog"`.
- Source: general (round 2, Issue 1)
- Disposition notes:

### M6 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs:250
- Description: `moduleCloseAskedHost` is read before the queued `close` event can fire.
  `nativeCloseAskedHost` is satisfied by the `RequestClose` that leaked from the earlier module
  close. The test added to pin M3 cannot fail on M5.
- Suggestion: wait a tick after `handle.Close()` before reading. Clear or count `calls` before
  the native `dialog.close()`.
- Source: general (round 2, Issue 2)
- Disposition notes:

## Duplicates / conflicts

- M5 and M6 are separate. M5 is the product defect, and M6 is the test gap that hid it. Fix
  them together.
