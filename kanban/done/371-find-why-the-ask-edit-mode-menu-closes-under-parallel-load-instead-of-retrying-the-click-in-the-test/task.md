# Find why the Ask edit-mode menu closes under parallel load, instead of retrying the click in the test

## Problem (2026-10-15)

During task 370 (parallel `dev test`), `tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs`
failed once in a 5× loop under parallel load. The edit-mode menu (task 355's pill) **closed between
the screenshot and the click**, and the test timed out after 30s.

370 "fixed" it by having the test **reopen the menu if it closed** before clicking. That's a retry. It
makes the test pass, but it can hide a real product race in which the menu closes unexpectedly, for
example from a re-render, focus loss, or a state change dismissing the popover. Task 351 showed this
pattern: the Tag "flake" was two real product races.

## Requirements

- **Reproduce deterministically:** loop the test under CPU throttling or parallel load, and capture
  what closes the menu. Possibilities include a component re-render that resets open state, a focus
  change triggering fluent-menu's light dismiss, a `StateHasChanged` from an unrelated State
  subscription, or the composer re-rendering the pill.
- **Fix the root cause in the product** if a real person could see it (the menu closing on its own),
  or in the test if it's test-only, using a deterministic signal. **Remove the reopen-and-retry**
  added in 370.
- Prove it with 50/50 loop passes under load, and check the sibling menus (the 356 settings menu,
  Tag) for the same cause.

## Goal (implementer restatement)

The Ask edit-mode menu can shut itself after it opens, before the person chooses an item. That showed up once under parallel `dev test`: the menu was gone between the screenshot and the click, and the test sat on a hidden item until it timed out. Task 370 made the test open the menu again. That hides the race.

Find what closes the menu. If a person can see it close on its own, fix that in the product. If only the test causes it, fix the test with a signal that does not click the pill a second time. Take out the reopen. Show the menu path passing 50 times under load, and check the settings menu and Tag for the same cause.

Done when:

- The cause is written down, and it says product or test.
- The reopen-and-retry from task 370 is gone, and the fix does not depend on trying the click again.
- The edit-mode menu path passes 50 times under load, and the settings menu and Tag were checked for the same cause.
- `dev build` is 0 warnings and 0 errors, the Playwright suite passes, and `ganda repo audit` passes.

Review round 1 left two open notes on the hint wrapper. The document observer and the `showPopover` wrap both set `popover="hint"`, and the popover type only matters when the tooltip is shown. FluentUI already shows every tooltip through `showPopover`, including the library's own `isConnected` override, which calls the method it captured. The extra observer is permanent work in every generated app and does not change the result. The microtask calls the wrap a second time after the prototype is already marked, so it returns immediately, and the Design text claims an order the library does not promise.

This pass is done when:

- `tooltip-hint.ts` keeps the `showPopover` wrapper and no longer watches the document.
- The microtask re-wrap is gone.
- The Design region says the wrapper composes with FluentUI's `isConnected` override in either order.
- The Ask edit-mode menu and the settings menu still stay open when the Copy tooltip is shown.
- `dev build` is 0 warnings and 0 errors, `AskSurface_Given_Wasm` passes, and `ganda repo audit` passes.

## Checklist

- [x] Reproduce + root cause recorded (product vs test)
- [x] Deterministic fix; 370's reopen retry removed
- [x] 50× loop under load green; sibling menus checked
- [x] Gates: `dev build` 0/0, Playwright suite, `ganda repo audit`

## Session

- Created: 2026-10-15 (cockpit, per Steve; from 370's retry in #514)
- Implementation: 2026-10-17. Product cause. The Copy tooltip's delayed `showPopover` closed the edit-mode menu. Hint popovers stop that. The reopen retry is gone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T10:24:12Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 2, session not exposed — 2026-10-17. Found 0 bugs, 1 suggestion, and 1 nit, all open. A fix pass is requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-17T10:28:29Z
- Fix pass (review round 1): 2026-10-17. Dropped the document observer and the microtask re-wrap. The hint is set inside `showPopover`, which composes with FluentUI's `isConnected` override in either order.
- Review oracle round 2: general reviewer (claude, model claude-opus-5-5), effort 1, session not exposed — 2026-10-17. M1 and M2 verified fixed, nothing new. Disposition: clean.

## Results

**Cause: product.** Under 32 CPU burners the edit-mode menu closed with `beforetoggle` newState `closed` and no click, key, scroll, or `HTMLElement.prototype.showPopover`. The next `toggle` was `fluent-tooltip` anchored to `AskCopy` (`popover=""`, text "Copy"). FluentUI's tooltip calls `showPopover` on a timer (immediately on focus, 250ms on hover) and does not check that the pointer is still on the button. An auto popover show light-dismisses every other auto popover, including the edit-mode menu. A person who copies an answer and then opens the menu can see the menu shut itself. The same show closes the settings menu (task 356), which is the same FluentMenu. Tag is not a menu. It is a `fluent-tooltip` source (`Tooltip` on the Tag button), so the same hint fix covers it. Task 351's Tag races were a different cause (the pick armed before `IsPicking`).

**Fix.** `tooltip-hint.ts`, imported by the SPA initializer, wraps `fluent-tooltip`'s `showPopover` and sets `popover="hint"` immediately before the captured show. A hint can show beside an open menu, and opening the menu dismisses the hint. The test no longer reopens the menu. It shows the Copy tooltip while the edit-mode menu is open and again while the settings menu is open, and requires the chosen item to stay visible.

**Review fix (round 1).** M1: removed the document `MutationObserver` (`watchTooltips` / `markHint`). FluentUI 5.0.0 shows a tooltip only through `this.showPopover()`, and its `isConnected` override calls the prototype method it captured, so the wrapper is the show path. M2: removed `queueMicrotask(wrapWhenDefined)`. The Design region now says the wrapper composes with that override in either order. Ledger: `review/round-1/merged.md` (M1 fixed, M2 fixed). Round 2 has not run.

**Proof.** `AskSurface_Given_Wasm` passed 50/50 under 32 CPU burners, with the Copy-tooltip assert on both menus. That load loop was the first implementation pass. It was not repeated after the review fix.

**Fix-pass proof.** `./bin/dev build --quiet` succeeded (warnings are errors). `ganda repo audit`: 32 passed, 0 failed. `AskSurface_Given_Wasm` passed 1/1 (`dotnet test -c Release --no-build --no-restore -- --filter-class AskSurface_Given_Wasm` from the Playwright project). `./bin/dev test --group playwright --no-build`: web-spa-playwright-tests 22/22.

**Review (round 1).** One general reviewer ran at effort 2. The reviewer confirmed the root cause against the FluentUI 5.0.0 bundle, where the tooltip sets `popover ??= "auto"` and shows from a timer. The reviewer re-ran `./bin/dev build` (passed) and `AskSurface_Given_Wasm` (1/1). The reviewer did not re-run the 50× load loop. Findings: 0 bugs, M1 (suggestion), M2 (nit). Both are fixed on `review/round-1/merged.md`. Framework: `review/review-framework.md`.

**Review (round 2) and disposition: clean.** One general reviewer ran at effort 1 on the fix delta (`b1fe9f792..bffc9d4f6`). In the FluentUI 5.0.0 bundle, both the library override and this wrapper sit on the `fluent-tooltip` prototype and call the function they captured, so they compose in either order. No tooltip show goes through `togglePopover`. The reviewer re-ran `./bin/dev build --quiet` (succeeded) and `AskSurface_Given_Wasm` (1/1, Release, `--no-build`). The reviewer did not re-run the 50× load loop. Final counts over 2 rounds: bug 0; suggestion 1 fixed (M1); nit 1 fixed (M2); 0 open, 0 wontfix. Paths: `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet test -c Release -- --filter-class AskSurface_Given_Wasm
```

Expect: the run passes. The edit-mode menu stays open when the Copy tooltip is shown, and so does the settings menu. There is no second click on the edit-mode pill before "Automatically edit". To repeat the load proof, start one `python3 -c 'while True: pass'` per CPU, run that test 50 times, then stop the python processes. Expect 50 passes.

## Review

- Round 1: M1 fixed, M2 fixed. Re-review is round 2.
- Round 2: Done
