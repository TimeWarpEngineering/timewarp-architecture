# Round 1 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/LineChart.razor:122
- Description: `LineChart` drops non-finite values, then places each remaining value by its index in the filtered list. `CategoryCount` also counts the filtered list. One `NaN` in the middle of a series moves every later point, and the last-point dot, one category to the left. With `[10, NaN, 30]` and labels A, B, C, the value 30 and the dot are drawn above B.
- Suggestion: Place each finite value at its original category index. Split the polyline at a gap, or join across it, and record the choice in the Design region. Size categories from `Points.Count`. Add a render test with `[10, NaN, 30]` that asserts the dot's x equals the x of the third label.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:62
- Description: The change deleted `.twe-role-description` (`margin: 0 0 16px`) and wrapped the description in a `DefinitionList` with `margin: 0`. `Section` does not stack its children, and the permission editor root has no top margin, so the description now sits flush against the permission groups. A blank description now shows a "Description" term with an empty value.
- Suggestion: Put the gap back, for example with `twe-stack` around the list and the editor, or a spacing class through `Class`. Optionally omit the item when the description is blank.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:73
- Description: The `AdminPrincipalDisplayName` and `AdminPrincipalEmail` test hooks moved from the value `<dd>` to the item wrapper, so their text now includes the term.
- Suggestion: Put each hook on a span inside the value.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/PeriodStepper.razor:40
- Description: The page changes the label after Previous or Next, and nothing announces the change to a screen-reader user.
- Suggestion: Add `aria-live="polite"` to the label span. Consider `role="group"` on the root.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. There was a single reviewer.

## Verified gates (reviewer re-run)

- `./bin/dev build`: 0 warnings, 0 errors, and the client-name scan passed.
- HtmlRenderer, Release: DefinitionList 5/5, LineChart 6/6, PeriodStepper 4/4, PageSplit 4/4, TableNumeric 2/2, IntegrationCard 10/10, DesignSystemPage 6/6.
- The restatement matches the task. No misread was found.
