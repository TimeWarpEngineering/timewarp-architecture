# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:342
- Description: `sg-follow` on a child component root was dead under CSS isolation.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Page-authored native `div.sg-follow` wrappers now hold the permission grid and the dev note, and `data-qa` moved with them. Verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/RoleCard.razor:6
- Description: The parent's roving tab stop and arrow-key duty were not named.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. The Design region and the `overview.md` row name the duty and point at `RovingSelection` and `SegmentedControl`. The sample sets `tabindex` 0 / −1. The test pins the `tabindex` passthrough.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/permission-grid-render-tests.cs:39
- Description: `ShouldContain("Granted")` also matched `Not granted`.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Both assertions now use `>Granted<`.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/PermissionGrid.razor:42
- Description: The hidden state span was named `__name`.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. It is now `twe-perm-grid__state` in the Razor and CSS files, and the test rejects the old name.

## New findings

None.

## Duplicates / conflicts

- None. A single reviewer.

## Operator notes (not findings)

- The base-branch overlap with `feature/379-portal-rest`, recorded in round 1, still stands. The operator decides the PR target.
