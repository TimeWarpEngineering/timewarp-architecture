# Round 2 — general
**Date:** 2026-10-23
**Scope reviewed:** fix delta `138a8cbea..93befc533` (`Drawer.razor`, `Drawer.razor.css`,
`drawer-js-module.cs`, `drawer.ts`, `drawer-playwright-tests.cs`), checked against round 1
M1–M4, the Goal restatement, and the original task.

## Summary

The fix pass adds a `wantOpen` flag (M1), logical insets and borders with a mirrored RTL slide
(M2), and a `close` listener (M3). M1 and M2 are correct. M3 adds the listener, but the
guard meant to skip closes the module starts does not work. `moduleClosing` is set only while
`dialog.close()` runs, and the browser fires `close` later, as a queued task. So every close the
module starts also calls `RequestClose`. The new Playwright check that should catch this reads
the result before the event has fired.

The Goal restatement matches the task and the round 1 findings. There is no misread.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:52
- Description: `closeFromModule` sets `moduleClosing = true`, calls `dialog.close()`, and resets
  the flag in `finally`. The HTML spec queues the `close` event as a task, so `onDialogClose`
  always runs after the flag is false again. Every close the module starts (`Close` when
  `IsOpen` turns false, and the `placeOnBody` close) then calls `RequestClose`, which calls the
  caller's `OnClose` a second time.
  Verified in the repo's Playwright Chromium (headless shell 1243). With
  `wwwroot/js/features/drawer.js` loaded, `Show()` then `Close()` gives 0 host calls right
  away and `["RequestClose"]` one timer tick later.
  Failure scenario, on the `ActiveModalId` pattern the Decision requires: a drawer is open
  and a control dispatches `SetActiveModal(otherId)`, for example a footer action that opens a
  confirm modal (379-009). The drawer's `IsOpen` turns false and `Close()` runs. The queued
  `close` then calls `RequestClose`, the drawer's `OnClose` dispatches `CloseModal`, and the
  modal that was just opened is cleared. In the simple case, Escape or the footer button
  dispatches `CloseModal` twice.
- Suggestion: do not tie the guard to the time `close()` takes. Two options:
  - In `onDialogClose`, return early when `wantOpen` is already false. `Close`, `Dispose`, and
    a superseded show clear it before they call `close()`. Have `placeOnBody` skip the close
    when it is only moving the element, or set a flag that the next `close` event clears.
  - Or record a pending module close that the `close` handler takes and clears.
  The M3 behaviour must not change: a `method="dialog"` close the module did not start still
  calls `RequestClose`.
- Status: open

### Issue 2 — Severity: bug
- File: tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs:250
- Description: `moduleCloseAskedHost` is read in the same task as `handle.Close()`, before the
  queued `close` event can fire, so it passes even with the defect in Issue 1.
  `nativeCloseAskedHost` uses `calls.includes("RequestClose")` on a list that gathers every
  call. The leaked `RequestClose` from the earlier module close satisfies it, so the check does
  not show that the native `dialog.close()` asked the host. The test was added to pin M3, but
  it pins neither half of the contract.
- Suggestion: wait a frame or a timer tick after `handle.Close()` before you read
  `moduleCloseAskedHost`. Clear `calls`, or count them, before the native `dialog.close()`.
- Status: open

## Re-verified prior findings

- M1 fixed: `Show` sets `wantOpen`. `Close` and `Dispose` clear it. The deferred `show`
  returns when the flag is clear (`drawer.ts:173`).
- M2 fixed: `inset-inline-*` and `border-inline-*` are used for End and Start. The `:dir(rtl)`
  slide swap sits inside `prefers-reduced-motion: no-preference`, so reduced motion still
  turns the slide off.
- M3: the `close` listener exists, and a native close reaches `RequestClose`. The skip for
  module closes is broken (Issue 1).
- M4: still wontfix. Not revisited.

Gates: this round did not re-run build, render tests, Playwright, template-smoke, or the
audit. The defect above was reproduced directly against the compiled module.
