# Round 2 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/LineChart.razor:126
- Description: A non-finite value moved later points one category to the left.
- Source: general (round 1)
- Disposition notes: fe8206a19. Each finite value is placed at its original index, the category count uses the raw list, and the line joins across the gap (recorded in the Design region). Pinned by `Keep_A_Gap_On_Its_Original_Category`.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:64
- Description: Role detail lost the gap under the description, and a blank description rendered an empty item.
- Source: general (round 1)
- Disposition notes: fe8206a19. `twe-stack` wraps the list and the editor. A blank description is omitted.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:73
- Description: The name and email hooks included the term text.
- Source: general (round 1)
- Disposition notes: fe8206a19. The hooks are on a span inside the value. Nothing else reads these hooks.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/PeriodStepper.razor:42
- Description: Nothing announced a label change to a screen reader.
- Source: general (round 1)
- Disposition notes: fe8206a19. `aria-live="polite"` on the label and `role="group"` on the root. Both are asserted in the render test.

## New findings

None.

## Duplicates / conflicts

- None. There was a single reviewer.
