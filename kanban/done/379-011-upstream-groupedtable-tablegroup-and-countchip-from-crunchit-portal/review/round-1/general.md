# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** same as framework (master...1a1cdf8c5, `source/` and `tests/`)

## Summary

The change adds three native leaves under `components/elements/`. `GroupedTable` is a paper container around one `<table class="twe-table">`, with `aria-label` from `Label` and a cascaded `ColumnCount`. `TableGroup` is a `<tbody>` whose first row is `th scope="rowgroup"` spanning the column count, holding the title, an accent `CountChip`, and the meta. `CountChip` is the neutral or accent pill. The change also adds a style guide sample, overview rows, and 12 HtmlRenderer tests. The implementation matches the Decision and Checklist. The one-table, `<tbody>`-per-group structure fixes the Crunchit defect. The CSS uses only `--twe-*` tokens, with no hex values and no `<style>`. The isolated header selector (0,2,1 with the scope attribute) beats the global `.twe-table th` (0,1,1). Risk is low. One design point is worth fixing: the column count cascades as a bare unnamed `int`. One Design comment makes a false claim about row hover.

The goal restatement matches the task: slot-based, one table, style guide only, no events and no collapse. There is no misread.

Verified locally:
- `dotnet build -c Release` of `web-spa-integration-tests`: 0 warnings, 0 errors
- `CountChip_Should_` 4/4 passed; `GroupedTable_Should_` 8/8 passed
- `git diff master...HEAD -- source tests | rg -i "crunchit|copic|bill"` prints nothing
- No hex literal in the three new `.razor.css` files

I did not re-run `dev build`, `dev template-smoke`, or `ganda repo audit` in this round. They are the implementer's reported gates, and the PR gate re-runs them.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/elements/TableGroup.razor:27 (and GroupedTable.razor:29)
- Description: `ColumnCount` cascades as an unnamed `CascadingValue<int>`, and `TableGroup` receives it with an unnamed `[CascadingParameter] int`. Blazor matches unnamed cascades by type, so the link is "the nearest int above me." This causes two problems.
  - A `TableGroup` rendered outside a `GroupedTable` (the stand-alone case the Design region supports) takes any unrelated `int` an ancestor cascades as its `colspan`, rather than falling back to 1.
  - Any component the page puts inside `GroupedTable`'s `ChildContent` (a cell component, or a third-party leaf) that declares an unnamed `[CascadingParameter] int` silently receives the column count.
  This template ships to every generated app, so the link should be explicit.
- Suggestion: Name the cascade on both sides, for example `<CascadingValue Name="TweGroupedTableColumnCount" Value="ColumnCount">` and `[CascadingParameter(Name = "TweGroupedTableColumnCount")]`. Another option is to cascade a small dedicated type. Keep the fallback to 1. Mention the name in both Design regions. Optionally add a render test showing that a stand-alone `TableGroup` under an unrelated `CascadingValue<int>` still spans 1.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/elements/GroupedTable.razor:13 (and GroupedTable.razor.css:3)
- Description: The Design region says "Row hover and cell padding stay on the global .twe-table rule." `wwwroot/css/app.css` has no row hover rule. `.twe-table` sets only width, collapse, font size, and the `th`/`td` padding and borders, and the only `:hover` in app.css is `a:hover`. The claim came from the spec, but a Design region is the record agents read, so a false claim there misleads the next change.
- Suggestion: Say "Cell padding and rules stay on the global .twe-table rule", or drop the hover mention, in the Design region and the CSS header comment.
- Status: open
