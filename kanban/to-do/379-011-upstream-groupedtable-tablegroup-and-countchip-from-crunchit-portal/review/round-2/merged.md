# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/TableGroup.razor:28 (and GroupedTable.razor:30)
- Description: `ColumnCount` cascaded as an unnamed `int`.
- Suggestion: Name the cascade on both sides.
- Source: general (round 1)
- Disposition notes: Fixed in d9678e5a8. `TableGroup.ColumnCountCascadeName` ("TweGroupedTableColumnCount") is used by both sides. Both Design regions record it. `Ignore_An_Unrelated_Int_Cascade_When_A_Group_Is_Rendered_Alone` pins `colspan="1"` under `CascadingValue<int>` 7. Re-run: 9 of 9 pass.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/GroupedTable.razor:14 (and GroupedTable.razor.css:3)
- Description: The comments claimed row hover from the global `.twe-table`.
- Suggestion: Drop "row hover".
- Source: general (round 1)
- Disposition notes: Fixed in d9678e5a8. Both comments say "cell padding and rules".

## New findings

None on the fix delta.

## Duplicates / conflicts

- None (single reviewer).
