# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** same as framework (98a89424f vs 6f19ce597): `tooltip-hint.ts`, `web.spa.lib.module.ts`, `ask-surface-playwright-tests.cs`, task.md Results.

## Summary

The change fixes a real product race. FluentUI 5.0.0's `fluent-tooltip` sets `this.popover ??= "auto"` in
`connectedCallback`. `showTooltip` calls `this.showPopover()` from a `setTimeout` (0 ms on focus, 250 ms
on hover), and nothing checks hover at show time. The test clicks Copy (`AssertCopyWritesTheLastAnswerAsync`)
just before it opens the edit-mode pill, so a late Copy show light-dismisses the open auto menu. I
confirmed this in the shipped bundle
(`~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0/staticwebassets/…lib.module.js`).
Making tooltips `hint` popovers is the right fix, and it sits at the right layer. The 370 reopen is
gone, and the new assert would fail without the fix (an auto show closes the menu, so the helper returns
`auto:closed`). The restatement matches the task, so there is no misread.

Gates I re-ran: `./bin/dev build --quiet` passed, and `AskSurface_Given_Wasm` passed 1/1 (Release,
`--no-build`). I did not re-run the 50× load loop or the full Playwright group. Engines without `hint`
support fall back to the attribute's invalid-value default, `manual`, which also does not light-dismiss
the menu, so I raised no finding for it.

The two findings are about simplicity and accuracy in a file that ships to every generated app.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:36
- Description: Two mechanisms do one job. The popover type only matters when `showPopover` runs, and
  every FluentUI tooltip show goes through `this.showPopover()`. That includes `showTooltip` and
  FluentUI's own `isConnected` override, which calls the prior prototype method in either install
  order. The prototype wrapper already sets `hint` right before each show. The document-wide
  `MutationObserver` (`childList` + `subtree` + `attributes` on `<html>`, with a `querySelectorAll`
  per added element) adds nothing to correctness. It does add a permanent observer to every generated
  app. The test calls `tooltip.showPopover()`, so it only exercises the wrapper path.
- Suggestion: Drop `watchTooltips` and `markHint` and keep only the `showPopover` wrapper. If there is a
  path the wrapper misses (for example native `HTMLElement.prototype.showPopover.call` or
  `togglePopover`), name it in the Design region and cover it in the test. Otherwise one mechanism is
  enough.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/tooltip-hint.ts:91
- Description: `queueMicrotask(wrapWhenDefined)` never does anything. `tweHintWrapped` is set on the
  prototype by the first call, so the second call returns early even if FluentUI later reassigns
  `prototype.showPopover`. The Design region says the wrap happens "after the library's own wrap".
  That ordering is not guaranteed: FluentUI's override sits behind an async `whenDefined` chain over
  three elements. It also does not matter, because each wrapper calls the one it captured. The region
  is the record, so a wrong ordering claim will mislead the next reader.
- Suggestion: Remove the microtask re-call. Reword the Design sentence to say that the wrap composes
  with FluentUI's `isConnected` override in either order.
- Status: open
