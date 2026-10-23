# Round 3 — merged findings
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
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:388
- Description: `sg-follow` on a child component root was dead under CSS isolation.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Still holds after merge `d94228528`. The native `div.sg-follow` wrappers are at lines 388 and 395, and `.sg-follow` survived the CSS conflict.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/RoleCard.razor:6
- Description: The parent's roving tab stop and arrow-key duty were not named.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Still holds after the merge. The overview row kept the roving-tabindex wording when its conflict was resolved.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/permission-grid-render-tests.cs:39
- Description: `ShouldContain("Granted")` also matched `Not granted`.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Unchanged by the merge. PermissionGrid 5/5 passes.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/PermissionGrid.razor:42
- Description: The hidden state span was named `__name`.
- Source: general (round 1)
- Disposition notes: Fixed in `8041a392f`. Unchanged by the merge.

## New findings

None. In the merge `d94228528`, all four conflict resolutions (StatusBadge, overview, style-guide CSS, tokens) kept both sides. Build, render tests, and CI are green.

## Duplicates / conflicts

- None. A single reviewer.

## Operator notes (not findings)

- The PR target question from rounds 1 and 2 is settled. PR #568 targets `feature/379-portal-rest`, and the branch merged that feature branch.
