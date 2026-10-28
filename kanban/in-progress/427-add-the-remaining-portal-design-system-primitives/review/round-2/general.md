# Round 2 — general
**Date:** 2026-10-28
**Scope reviewed:** fix delta `247c4282a..f06c3c019` (product commit fe8206a19). Paths: `LineChart.razor`, `PeriodStepper.razor`, `PrincipalDetailPage.razor(.cs)`, `RoleDetailPage.razor(.cs)`, `line-chart-render-tests.cs`, `period-stepper-render-tests.cs`.

## Summary

The fix pass targets the four round-1 findings and changes nothing else. `LineChart` now puts each finite value at its original index. `CategoryCount` counts the raw point list. The line joins across a non-finite gap, and the Design region records that choice. `Scale()` still reads only finite values, so a `NaN` can't distort the y range. Role detail wraps the list and the editor in the global `twe-stack` (`app.css:70`, 16px gap) and omits a blank description. The principal hooks moved onto a span inside the value. No test or page selector reads `AdminPrincipalDisplayName`, `AdminPrincipalEmail`, or `RoleFacts`, so nothing breaks. The period label is `aria-live="polite"` and the root is `role="group"`. The new tests pin the gap case (dot x equals label C; vertices are A and C) and the two ARIA attributes. This round raised no new issues.

## Prior findings re-verified

- M1 — fixed. `LineChart.razor:126-158`: the loop skips non-finite values but uses the raw index for `XAt`. `lastX`/`lastY` track the last finite value. A series with no finite values is still omitted (`pairs.Count == 0`). `Keep_A_Gap_On_Its_Original_Category` passes.
- M2 — fixed. `RoleDetailPage.razor:64-72`: `twe-stack` restores the 16px gap. The blank description is guarded by `IsNullOrWhiteSpace`.
- M3 — fixed. `PrincipalDetailPage.razor:73-74`: the hooks are on a span inside the value.
- M4 — fixed. `PeriodStepper.razor:37,42`: `role="group"` on the root and `aria-live="polite"` on the label. Tests assert both.

## Issues

None.

## Verified gates (reviewer re-run)

- `./bin/dev build`: 0 warnings, 0 errors.
- HtmlRenderer, Release: LineChart 7/7, PeriodStepper 4/4, DefinitionList 5/5, DesignSystemPage 6/6.
- The implementer restatement still matches the task. No misread was found.
