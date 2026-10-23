# Round 2 — general
**Date:** 2026-10-23
**Scope reviewed:** fix delta `7819e05b7..0f55dc6da` (`Drawer.razor`, `Drawer.razor.css`, `source/features/drawer.ts`, `drawer-playwright-tests.cs`, `task.md`), plus the unchanged `services/drawer-js-module.cs` Close / Dispose call sites

## Summary

The fix pass rewrites the Design regions in `drawer.ts`, `Drawer.razor`, and `Drawer.razor.css`. The Tab wrap is now there because this Chromium does not keep Tab inside a modal dialog. The inner panel is there because it is the flex column and the sheet hit target. `Dispose` removes its listeners and then calls `dialog.close()` directly. The `disposed` checks in the removed listeners are gone, and `pendingModuleClose` counts `Close` only. The Playwright test adds a probe: a modal dialog of only native buttons, under a `display:contents` host, for both `display:block` and `display:flex`. It also adds a Dispose-while-open check (no `RequestClose`, and a later `Show` stays shut).

Re-verified in this worktree: `StyleGuideDrawer_Given_Wasm` 1/1 passed (12.6s), and that run includes the new probe and the Dispose assertions. `drawer.ts` 163-182: `disposed` is now read only by `Show`. `closeFromModule` is used only by `Close`. After `Dispose` closes the dialog, a later `Close` is a no-op because the dialog is not open, so the count cannot leak. The CSS sets `padding: 0` on the open dialog, so the claim that the panel fills the dialog and a click whose target is the dialog is the backdrop holds. I did not re-run `./bin/dev build`, `template-smoke`, or `ganda repo audit` this round. The Playwright build compiled web-spa with no reported errors.

Goal restatement vs task: the fix pass added the three review-driven sentences to the restatement: the native-button Tab probe, the inner panel's reason, and Dispose without an unread guard. These narrow Goal 4 ("record why each remaining piece exists") and do not depart from it. No misread.

## Prior findings

- M1 (bug): fixed. `drawer.ts:10-14`, `Drawer.razor:28-33`, and Results now name the real cause. The Playwright probe `AssertNativeModalDisplayLetsTabLeaveAsync` keeps that evidence under test.
- M2 (suggestion): fixed. The Razor and CSS Design regions no longer say flex drops a Tab trap. The panel's reasons are the layout and the hit target, and the probe covers `display:flex`.
- M3 (suggestion): fixed. The dead `disposed` guards in `onKeyDown` and `onDialogClose` are gone. `Dispose` no longer increments `pendingModuleClose`. The Design line now says `pendingModuleClose counts Close`.

## Issues

None new on the fix delta.
