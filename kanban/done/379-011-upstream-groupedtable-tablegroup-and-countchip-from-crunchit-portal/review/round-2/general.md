# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** fix delta `c9f69e6f7..d9678e5a8` (commit d9678e5a8, "name the grouped-table column cascade"): `GroupedTable.razor`, `GroupedTable.razor.css`, `TableGroup.razor`, `grouped-table-render-tests.cs`, and the task.md restatement and Results edits

## Summary

The fix pass names the column cascade on both sides with one constant (`TableGroup.ColumnCountCascadeName = "TweGroupedTableColumnCount"`). `GroupedTable` passes it as `CascadingValue Name=`, and `TableGroup` reads it with `[CascadingParameter(Name = …)]`, so an unnamed ancestor `int` no longer sets the span and a descendant no longer receives the count. A new HtmlRenderer case renders a stand-alone group under `CascadingValue<int>` 7 and asserts `colspan="1"`. The "row hover" claim is gone from the Design region and the CSS header, which now say "cell padding and rules". Both Design regions record the name. Re-verified: `GroupedTable_Should_` passes 9 of 9 (Release build, `--filter-class`). No other mention of row hover remains in the three components. The only `ColumnCount` consumer outside the components is the style guide sample, which passes it as a parameter, so the rename does not affect it.

The goal restatement now names the cascade and the unrelated-int fallback. That matches the original task's intent (the page passes the count once and groups inherit it), so it is not a misread.

## Prior findings

- M1 (suggestion): fixed. Named cascade on both sides, shared constant, Design regions updated, regression test added.
- M2 (nit): fixed. Both comments now say "cell padding and rules".

## Issues

None.
