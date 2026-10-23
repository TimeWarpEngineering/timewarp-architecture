# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/TableGroup.razor:27 (and GroupedTable.razor:29)
- Description: `ColumnCount` cascades as an unnamed `int`. A stand-alone `TableGroup` takes any unrelated ancestor `int` cascade as its colspan. Any descendant in `ChildContent` with an unnamed `[CascadingParameter] int` receives the column count.
- Suggestion: Use a named cascade on both sides (for example `TweGroupedTableColumnCount`) or a dedicated type. Keep the fallback to 1, record the name in both Design regions, and optionally add a render test for a stand-alone group under an unrelated `CascadingValue<int>`.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/GroupedTable.razor:13 (and GroupedTable.razor.css:3)
- Description: The Design region and the CSS header say row hover comes from the global `.twe-table`. app.css has no row hover rule.
- Suggestion: Drop "row hover" from both comments, or say "cell padding and rules".
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
