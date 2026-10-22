# Round 3 — general
**Date:** 2026-10-23
**Scope reviewed:** fix delta `5d03e880b..92103e67a` (`drawer.ts`, `Drawer.razor` Design region,
`drawer-playwright-tests.cs`), checked against round 2 M5 and M6, the Goal restatement, and the
original task.

## Summary

The fix replaces the `moduleClosing` flag, which was cleared when `close()` returned, with a
`pendingModuleClose` count. `closeFromModule` adds one before `close()`, and the queued `close`
listener takes one and returns. Each `close()` on an open dialog queues exactly one `close`
event, and those events run in order, so the count pairs each module close with its own event.
The Playwright check now waits for the queued event before it reads the host calls, and it
counts the native close on a cleared list. The Design regions in `drawer.ts`, `Drawer.razor`,
and the test file describe the new behaviour.

The Goal restatement matches the task and the round 2 findings. There is no misread.

## Issues

None.

## Re-verified prior findings

Checked against the compiled `wwwroot/js/features/drawer.js` (it contains `pendingModuleClose`)
in the repo's Playwright Chromium (headless shell 1243, driver 1.63.0). `fluent-button` was
defined as a stub so `Show` opens at once. Host calls were read 20 ms after each step:

| Scenario | Host calls |
|----------|------------|
| `Show`, then `Close` | none |
| Native `dialog.close()` while open | `RequestClose` once |
| `Close`, `Show`, then native `close()` in the same task, before either event runs | `RequestClose` once |
| `Show` on a dialog already open under a non-body parent (the `placeOnBody` close) | none; the dialog is open on `document.body` |
| Native `close()` after that move | `RequestClose` once |
| `Dispose` while open | none |

- M5: fixed. A close the module starts no longer calls `RequestClose`, including when the queued
  event runs after a reopen. A native close still does.
- M6: fixed. `waitForQueuedClose` adds its listener after `Bind`, so the module's listener runs
  first, and the check reads the count after it. With the round 2 flag back in place,
  `moduleCloseRequestCount` would be 1 and the test would fail. The native check clears `calls`
  first and requires exactly 1. If the dialog is not open, the helper resolves at once and the
  count stays 0, so that check cannot pass by accident.
- M1–M3: fixed in earlier rounds. This delta does not touch `wantOpen` or the CSS.
- M4: still wontfix. Not revisited.

`Dispose` removes the `close` listener before its `closeFromModule`, so the count it adds is
never taken. Nothing reads the count after `Dispose`, so this has no effect.

Gates: this round did not re-run build, render tests, the Playwright suite, template-smoke, or
the audit. The fix pass reports all of them green (task.md Session). The browser check above ran
the compiled module directly.
