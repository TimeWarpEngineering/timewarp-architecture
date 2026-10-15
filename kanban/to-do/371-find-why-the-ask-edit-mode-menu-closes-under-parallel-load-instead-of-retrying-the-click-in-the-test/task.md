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

## Checklist

- [ ] Reproduce + root cause recorded (product vs test)
- [ ] Deterministic fix; 370's reopen retry removed
- [ ] 50× loop under load green; sibling menus checked
- [ ] Gates: `dev build` 0/0, Playwright suite, `ganda repo audit`

## Session

- Created: 2026-10-15 (cockpit, per Steve; from 370's retry in #514)
