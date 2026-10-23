# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** same as framework (`8151be94b..f5eccc471`), plus the unchanged `services/drawer-js-module.cs` call sites

## Summary

The change removes `placeOnBody`, the close it started, and the `wantOpen` / `whenDefined("fluent-button")` deferral. `Show` now calls `showModal` on the same turn. The Playwright test now checks that the dialog stays under `.twe-shell`, matches `:modal`, paints over the app bar, keeps focus from the opener, and leaves the page cleanly while open. The behavior change is sound, and the claim that FluentUI defines components in `beforeStart` holds (5.0.1 `lib.module.js` has `beforeStart` / `beforeWebStart` calling `defineComponents`).

Re-verified in this worktree: `Drawer_Should_` 7/7 passed. `StyleGuideDrawer_Given_Wasm` 1/1 passed. `./bin/dev build` reported 0 warnings and 0 errors. I did not re-run `template-smoke` or `ganda repo audit`.

The open problem is the rule the task exists to fix: the Design regions must give the real reason for each remaining piece. The new reason given for the Tab wrap is wrong.

Goal restatement vs task: the restatement matches the task (move probe, removal or proof, the navigate-away test, slimming, and the 379-008 contracts). No misread.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:10 (also `components/elements/Drawer.razor:30-32` and task.md Results "What remains")
- Description: Design says the Tab wrap exists because "the same Chrome does not wrap Tab out of a fluent-button". The fluent-button is not the cause. I probed the same HeadlessChrome/153.0.8010.12 with Playwright 1.63.0, using a plain `<dialog>` under a `display:contents` div that holds only two native `<button>`s, opened with `showModal()`. Tab from the last button went `BODY -> a -> b -> BODY`. Chromium does not keep Tab inside a modal dialog at all: focus leaves to the document or browser chrome. Goal 4 asks Design to record why each remaining piece exists, and this task was filed because an earlier Design claim had never been checked. This is another unchecked cause, now presented with probe evidence that does not isolate it. The implementer's control ("listener skipped → BODY") is consistent with either explanation.
- Suggestion: Restate the reason in `drawer.ts`, `Drawer.razor`, and the Results: a modal dialog in this Chromium does not keep Tab inside, even with only native buttons, so the module wraps Tab. Optionally add a plain-button control to the Playwright probe or the Results so the cause stays verified.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:28-30
- Description: "The dialog itself stays display:block; a flex column on the dialog element drops the browser's Tab trap, so the column lives on an inner panel." The same probe found no Tab trap with either `display:block` or `display:flex` on the dialog. Both give `BODY -> a -> b -> BODY`. That text was already there, but it is the stated reason for the inner `twe-drawer__panel` wrapper, and Goal 4 asks for the true reason for each remaining piece.
- Suggestion: Drop the Tab-trap reason. Either give the real reason for the inner panel (for example, layout or the backdrop-click hit test, where "a click inside the panel hits a child") or say that the panel is a layout choice only.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:123, 150, 179 (Design line 19)
- Description: The task asks to slim `drawer.ts` to what the browser does not give. `Dispose` removes the `keydown` and `close` listeners before it calls `closeFromModule`. That leaves dead code: the `disposed` guard in `onKeyDown`, the `disposed` guard in `onDialogClose`, and the `pendingModuleClose += 1` that `Dispose` adds. Once a listener is removed, an event queued for it is never delivered to it. Design says "pendingModuleClose counts Close and Dispose", but the count from Dispose is never read.
- Suggestion: Remove the two listener `disposed` guards. Have `Dispose` call `dialog.close()` when open, not `closeFromModule`, or keep `closeFromModule` and fix the Design line to say only `Close` is counted. Keep the `disposed` guard in `Show`.
- Status: open
