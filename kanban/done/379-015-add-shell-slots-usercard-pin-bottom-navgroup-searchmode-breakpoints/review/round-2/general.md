# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** `b6bd9ba6d..27b7623a9`, the fix pass for round 1. That covers
`page-source-generator.cs`, `NavMenu.razor` and its CSS, `shell-rail.ts`, `ShellUserCard.razor`,
`TimeWarpPage.razor` and its CSS, `UserCard.razor.css`, `tokens.css`, and the generator, render,
breakpoint, and Playwright tests.

## Summary

The pass fixes six of the seven round 1 findings. The card name is now computed on each render,
the menu scrolls inside the column, the rail clips labels instead of removing them, the tooltip
appears only in the rail, the duplicate padding is gone, and the nav width reads the token.
M2 is still a bug in production. The generator never sees the generated `NavPlacement` enum,
so `GetConstantValue` has no value for a const field or a cast. Those two forms fall through to the
name check and silently become Top. The new generator test passes only because it declares the
enum in source, which a real app does not do.

Re-run by the reviewer: `./bin/dev build` (0 warnings, 0 errors), `PageSourceGenerator_Tests`
(36 passed), and `TimeWarpPage_Should_` (14 passed). The Playwright rail check and template-smoke
were not re-run.

## Prior findings re-verified

- M1 — fixed. `DisplayName` reads `ProfileState.Alias` on each render (`ShellUserCard.razor:29`).
  `Show_The_Profile_Alias_When_It_Arrives_After_The_First_Render` drives a ProfileState re-render
  and passes.
- M2 — still open. See Issue 1.
- M3 — fixed. `.twe-nav-menu` has `overflow-y: auto` (`NavMenu.razor.css:14`). The breakpoint file
  test and the Playwright rail check pin it.
- M4 — fixed. In the rail, labels and the card meta are visually hidden rather than set to
  `display: none`. `ItemTooltip` sets the title only while `Rail` is true.
- M5 — fixed. `Rail_Should_KeepTheCardClearOfTheMenu` checks 1024×700 and 1400×800. The chevron
  rail mark for categories is recorded in the NavMenu Design region. The check was not re-run in
  this round.
- M6 — fixed. The `::deep` copy is removed from `TimeWarpPage.razor.css`.
- M7 — fixed. `Width="var(--twe-shell-nav-width)"`, pinned by the breakpoint file test.

## Issues

### Issue 1 — Severity: bug
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:689
- Description: Carries M2. `BuildAttribute` emits the `NavPlacement` enum through
  `RegisterSourceOutput` (line 105). The syntax provider's semantic model therefore never binds
  it. A probe against Roslyn without the enum in source gave these results:
  `Placements.Pin` (a `const NavPlacement` field) returned `HasValue=False`. So did
  `(NavPlacement)1` and `NavPlacement.Bottom`. The fallback then gave `"Pin"` for the const field
  and `null` for the cast. Both page as Top with no diagnostic, which is the defect M2 reported.
  The Design region (lines 60–64) says a const field and a cast "count when the constant is known",
  but in an app the constant is never known. `Should_Emit_NavPlacement_Bottom_From_A_Compile_Time_Constant`
  and `Should_Keep_NavPlacement_Top_When_A_Known_Bottom_Member_Is_Zero` declare the enum in the
  test source, so they model a compilation the generator never receives.
- Suggestion: Pick one approach. (a) Resolve the forms that do not bind. For a const field, follow
  the field symbol from `GetSymbolInfo` (or its candidates) to its declaring initializer and
  recurse. For a cast, read the operand's literal value. (b) Accept only `NavPlacement.Top/Bottom`
  and using-static `Top/Bottom`, and report a diagnostic for any other expression. Either way,
  rewrite the Design region to describe what the generator really sees. Also make the const-field
  and cast tests run without declaring the enum in source, as an app compiles.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/shell-rail.ts:19
- Description: `query` and `onChange` are module-level, and `import()` returns one shared module
  instance. If a second `NavMenu` calls `watch`, it replaces the first listener. When the first
  instance is disposed, its `stop()` then removes the second instance's listener. Today one
  `TimeWarpPage` is live at a time, and the old instance is disposed before the new one's
  `OnAfterRenderAsync` runs, so the current shell works. The sibling `drawer.ts` returns a
  per-instance handle from `Bind` instead.
- Suggestion: Have `watch` return a handle (an `IJSObjectReference` with `stop`) and dispose that
  handle in `NavMenu.DisposeAsync`, the same way `drawer.ts` does.
- Status: open
