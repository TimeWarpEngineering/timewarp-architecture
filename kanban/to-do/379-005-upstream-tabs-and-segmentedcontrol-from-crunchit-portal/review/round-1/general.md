# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** same as framework (`master...836d14c45`, source and tests)

## Summary

The change adds `Tabs` and `SegmentedControl` as native-root composites with `Items`,
`OptionText`, `Value`, `ValueChanged`, `AriaLabel`, `Class`, and captured attributes, plus a
pure `RovingSelection` helper, style-guide cards, an overview entry, and render and helper
tests. It matches the checklist and the implementer restatement (no misread). Tokens all exist
in `tokens.css`; no hex, no `<style>`, no new token, no client name. Re-run here:
`Tabs_Should_` 4/4, `SegmentedControl_Should_` 4/4, `RovingSelection_Should_` 9/9; the
client-name grep prints nothing. The main risk is the new inline `onkeydown` script on each
root: it cancels keys the C# handler then ignores, and its key list is a second copy of
`FromKey`'s switch kept in step only by a comment.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:99 (same at SegmentedControl.razor:97)
- Description: The root's inline `onkeydown` calls `preventDefault()` for ArrowLeft, ArrowRight, Home, and End without checking modifiers. `OnKeyDownAsync` (Tabs.razor:80, SegmentedControl.razor:78) returns early for Alt, Ctrl, Meta, or Shift. A modified chord is therefore cancelled and then does nothing. With focus on a tab or segment, Alt+ArrowLeft / Alt+ArrowRight (browser Back / Forward on Windows and Linux) and Ctrl+Home / Ctrl+End (page scroll) stop working. The Design region says "Modified chords are ignored", but the browser default is still blocked.
- Suggestion: Make the script skip modified chords the same way the C# handler does, for example `if(!event.altKey&&!event.ctrlKey&&!event.metaKey&&!event.shiftKey&&(…))event.preventDefault()`. Then the cancelled set is exactly the handled set. Pin it with a render test (see Issue 2).
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/composites/roving-selection.cs:61 (and the two root `onkeydown` strings)
- Description: The keys the browser cancels (two hand-typed JS strings, one per component) and the keys `FromKey` handles (a C# switch) must agree. Today only a Design-region sentence ("Those four names are the keys FromKey handles") keeps them in step. AGENTS.md hard rule: "When two things must agree, generate one from the other or add a build check." Adding a key to `FromKey` (for example ArrowUp/ArrowDown for a vertical variant) without editing both scripts lets the page scroll as the selection moves. Issue 1 is a live case of the two drifting (on modifiers).
- Suggestion: Put the key names in one place in `RovingSelection` (constants used by the `FromKey` switch). Build the cancel script from them as one `internal const`/static string that both roots bind (`onkeydown="@RovingSelection.CancelMoveKeysScript"`). Add a test asserting the rendered root `onkeydown` contains each handled key and the modifier guard.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:22 and SegmentedControl.razor:20
- Description: The two `@code` blocks are byte-identical, about 80 lines each: the parameters, `ItemReferences`, `PendingFocus`, `TabStopIndex`, `OnParametersSet`, `OnAfterRenderAsync`, `ChooseAsync`, and `OnKeyDownAsync`. The keyboard and focus Design prose is duplicated too. Any fix (Issues 1, 2, 4) has to be made twice, and the copies can drift. The test files duplicate `RenderAsync`, `RootTag`, `Buttons`, and the selected/tabindex parsers in the same way.
- Suggestion: Move the shared state and behavior into one base (for example an abstract `RovingSelectionGroup : ComponentBase` in `components/composites/`, with the Design region there). Each `.razor` keeps only its markup and role/aria names via `@inherits`. Optionally share the test parsing helpers in one test-local static class.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:85 (same at SegmentedControl.razor:83)
- Description: The pattern variable `MoveResult` is method-scope but PascalCase, and it shadows the type name `RovingSelection.MoveResult`. The tw-csharp convention is class-scope PascalCase, method-scope camelCase.
- Suggestion: Rename it to `move` (or `moveResult`).
- Status: open
