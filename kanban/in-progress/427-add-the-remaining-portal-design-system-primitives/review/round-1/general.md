# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** `master...HEAD` (e3c76a611): every source, CSS, sample, catalog, overview, and test file in the diff, plus `GroupedTable`, `TableHeader`, `Cell`, `Section`, `RolePermissionEditor`, and tokens.

## Summary

The change adds the six primitives the task asks for. Each one follows the leaf rules: a native root, `Class` appended, unmatched attributes splatted, `--twe-*` tokens, and Purpose and Design regions. Each has a catalog part or an update to an existing part, a usage snippet, an overview row, and HtmlRenderer tests. I re-ran the gates. `./bin/dev build` reported 0 warnings and 0 errors, and its client-name scan passed. All seven named test classes passed: DefinitionList 5, LineChart 6, PeriodStepper 4, PageSplit 4, TableNumeric 2, IntegrationCard 10, and DesignSystemPage 6. The implementation matches the restatement, and the restatement matches the task. The main defect is in `LineChart`: when a series contains a non-finite value, every later point moves left onto the wrong axis label. The other findings are a spacing regression on role detail and two small accessibility and test-hook points.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/elements/LineChart.razor:122
- Description: `Plotted()` calls `Usable(series)`, which drops non-finite values (line 234). It then places each remaining value at `XAt(index, categories)` using its index in the filtered list (line 137). `CategoryCount` also counts the filtered list (line 93). As a result, one `NaN` in the middle of a series moves every later point one category to the left. For example, take `Points = [10, NaN, 30]` with `AxisLabels = ["A", "B", "C"]`. The value 30 is drawn above "B", and the last-point dot is also above "B" instead of "C". The Design region says the plot accepts non-finite values (it drops a series that has none that are finite), so this input is handled, and the result is a wrong plot. No test covers a gap in the middle of a series.
- Suggestion: Keep each value's original category index. Skip non-finite values, but place each finite value at `XAt(originalIndex, …)`. Split the polyline at a gap, or join across it, and record which one in the Design region. Size `CategoryCount` from `Points.Count`, not from the filtered count. Add a render test with `[10, NaN, 30]` and three labels that asserts the dot's `cx` equals the x of the third label.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:62
- Description: Role detail never had a two-column fact grid. It had `<p class="twe-role-description">` with `margin: 0 0 16px`. The change deletes that stylesheet and wraps the description in a one-item `DefinitionList` (`margin: 0`). `Section` does not stack its children, and the root of `RolePermissionEditor` (`.twe-permission-editor`) has no top margin. The description now sits flush against the permission groups, so the 16px gap is lost. Also, a role with a blank description now shows a "Description" term with an empty value, where it used to show an empty paragraph.
- Suggestion: Put the space back. One way is to wrap the `DefinitionList` and the editor in `twe-stack`. Another is to give this page's list a spacing class through `Class`. Optionally, render the item only when `Role.Description` is not blank.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:73
- Description: `data-qa="AdminPrincipalDisplayName"` and `data-qa="AdminPrincipalEmail"` used to sit on the `<dd>`. They now sit on the `DefinitionItem` wrapper `div`, so the element's text includes the term ("Display name…", "Email…"). No current test reads these hooks. The only Playwright use is `AdminPrincipalStatus`, which is still on the list root. A future text assertion on these hooks would get the term as well as the value.
- Suggestion: Put the hook on the value: `<DefinitionItem Term="Email"><span data-qa="AdminPrincipalEmail">@OrDash(profile.Email)</span></DefinitionItem>`.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/PeriodStepper.razor:40
- Description: After a press on Previous or Next, the page changes `Label`, but nothing tells a screen-reader user that the period changed. Focus stays on the button, and the label span is not a live region.
- Suggestion: Add `aria-live="polite"` to the label span. Consider `role="group"` on the root, with an `aria-label` that the caller passes through the splatted attributes.
- Status: open
