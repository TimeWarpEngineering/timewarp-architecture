# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix-pass delta `ce48b23dd..2d4892b38` (product paths only): `Tabs.razor`,
`SegmentedControl.razor`, `roving-selection.cs`, new `roving-value-group.cs`, new test helper
`roving-markup.cs`, and the three render/helper test files.

## Summary

The fix pass moves the shared parameters, focus, choose, and key handling onto an abstract
`RovingValueGroup : ComponentBase`. Both razor files now `@inherits` it and keep only their markup
and ARIA roles. `RovingSelection` holds one `HandledKeys` list. `CancelScript` is built from it with
an `!altKey&&!ctrlKey&&!metaKey&&!shiftKey` guard. `FromKey` refuses any key that isn't on the list,
and both roots bind `CancelScript`. Render tests read the rendered `onkeydown` and check it equals
`CancelScript` and names every handled key and every modifier flag. The risk is low. No new defects.

## Re-verification of round 1

- **M1 (bug) — fixed.** `roving-selection.cs:329-339` emits the modifier guard. The C# handler
  (`roving-value-group.cs:429-434`) ignores the same four flags, so the browser cancel and the
  handler now cover the same key set. `Cancel_Unmodified_Arrow_Home_And_End_Only` in both render
  suites pins the rendered guard.
- **M2 (suggestion) — fixed.** `HandledKeys` is the only list. The script is generated from it, and
  `FromKey` gates on `IsHandled` and switches on the same constants. A key added to the list without
  a switch arm returns `-1`. That makes `FromKey_Handles_Each_Cancelled_Key_…` fail, so the two
  can't drift silently.
- **M3 (suggestion) — fixed.** The duplicated `@code` blocks are gone. The one shared Design prose
  lives in `roving-value-group.cs`. `RovingMarkup` shares the test parsers. `@ref="ItemReferences[slot]"`
  still writes into the base's backing array through the read-only property. CSS isolation is
  unaffected, because each razor file still owns its native root.
- **M4 (nit) — fixed.** The pattern variable is `move` (`roving-value-group.cs:436`).

## Verification run (this round)

- From `tests/container-apps/web/web-spa-integration-tests`, `dotnet test -c Debug -- --filter-class`:
  `Tabs_Should_` 5/5, `SegmentedControl_Should_` 5/5, `RovingSelection_Should_` 10/10. The build
  that feeds them is clean (no warnings surfaced).
- `git diff master -- source tests | grep -ci crunchit` → 0.

## Issues

None.
