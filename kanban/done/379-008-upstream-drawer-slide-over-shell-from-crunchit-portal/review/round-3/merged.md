# Round 3 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:175
- Description: a deferred `showModal` could open after `Close`.
- Source: general (round 1)
- Disposition notes: fixed in 93befc533 (`wantOpen`). Re-verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor.css:37
- Description: End and Start used physical CSS.
- Source: general (round 1)
- Disposition notes: fixed in 93befc533. Re-verified in round 2.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:156
- Description: there was no `close` listener for a native close the module did not start.
- Source: general (round 1)
- Disposition notes: listener landed in 93befc533. Its skip for module closes was fixed in
  92103e67a (M5). A native close calls `RequestClose` once, re-verified in round 3.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:112
- Description: `Show` or `Close` interop runs on every render.
- Source: general (round 1)
- Disposition notes: wontfix (review oracle, round 1). The call keeps the dialog in sync with
  `IsOpen`, and it costs little. See round-1/merged.md.

### M5 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:50
- Description: `moduleClosing` was reset before the queued `close` event fired, so every close
  the module started also called `RequestClose`.
- Source: general (round 2)
- Disposition notes: fixed in 92103e67a with the `pendingModuleClose` count. Re-verified in round 3
  in Chromium: `Close`, `Dispose`, and the `placeOnBody` close make no host call, and a native
  close makes one, including a native close queued behind a module close.

### M6 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs:261
- Description: the check read the result before the queued `close` event fired, and an earlier
  module close could satisfy the native-close check.
- Source: general (round 2)
- Disposition notes: fixed in 92103e67a. The check waits for the event, and it counts the
  native close on a cleared list, requiring exactly 1. Re-verified in round 3.

## Duplicates / conflicts

- None. Round 3 raised no new findings.
