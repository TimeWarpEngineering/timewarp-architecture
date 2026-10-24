# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/components/ShellUserCard.razor:29
- Description: The display name is cached in `OnParametersSetAsync`, which runs only when the
  auth task changes. The profile alias arrives later through `FetchProfileData`, or changes
  through `UpdateProfile`. Either way it causes a state re-render, not a parameter set. The card
  keeps the identity name, while the email detail updates.
- Suggestion: Compute the name in markup from the current `ProfileState.Alias`. Keep only
  the auth-derived values in `OnParametersSetAsync`. Add a render test for an alias that changes
  after the first render.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:317
- Description: NavPlacement is read by the syntax member name `Bottom`. A const field,
  `using static`, or a cast silently becomes `Top`, with no diagnostic. The Design region says
  any compile-time constant is accepted.
- Suggestion: Use `semanticModel.GetConstantValue` (underlying value 1), or add a diagnostic and
  correct the Design region. Add a generator test that uses a const field.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor.css:6
- Description: `.twe-nav-menu` has `min-height: 0` and no overflow inside a column that is
  100% tall. A menu taller than the column shrinks, and its links overflow onto the pinned user
  card. This was not browser-verified.
- Suggestion: Add `overflow-y: auto` to `.twe-nav-menu`, and cover it with the M5 Playwright check.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:78
- Description: In the rail, `display: none` on the nav labels and the card meta leaves the links
  and the linked card without an accessible name. At full width, the tooltip repeats the visible
  label.
- Suggestion: Hide the text visually instead, or add `aria-label`. Show the tooltip only in the rail.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css:146
- Description: The rail has never rendered at a real width. Overlap, subitem indentation, and
  icon-less categories (chevron only) are unverified.
- Suggestion: Add a Playwright viewport check at about 1024x700 to the first-paint suite:
  68px nav, hidden labels, and no overlap between the card and the menu. Give categories icons,
  or document the chevron.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css:135
- Description: The `.twe-nav-column__user` padding is written three times. The `::deep` copy
  duplicates ShellUserCard's isolated rule.
- Suggestion: Drop the `::deep` copy.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:95
- Description: Nothing reads `--twe-shell-nav-width`. The shell hard-codes `Width="240px"`.
- Suggestion: Bind the width to the token, or note that the token is documentation only.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- There was one reviewer, so there are no duplicates. The overlap risk in M3 also motivates the
  browser check in M5. They stay separate because M3 is a CSS defect with a one-line fix.
