# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor.css:35
  (also `SegmentedControl.razor.css:35`, `PeriodStepper.razor.css:29`, `FilterSelect.razor.css:58`,
  `FilterBar.razor.css:56`, `AskReferencePicker.razor.css:33`, `AgentAsk.razor.css:372`,
  `wwwroot/css/app.css:113`)
- Description: Focus rings and focused-field borders read `--twe-primary` (`#55409c`), which the
  dark block keeps. That is about 1.6:1 on dark paper `#303030`, under WCAG 1.4.11's 3:1. Keyboard
  focus is near-invisible in dark mode. `RoleCard`'s focus ring was already moved to a role token in
  this diff, so the rest are inconsistent with it.
- Suggestion: Add a `--twe-focus-ring` role token: light = `var(--twe-primary)`, so light mode is
  unchanged, and dark = a light brand step such as `var(--twe-purple-soft)`. Use it for the listed
  focus outlines and borders, and optionally for the active-tab underline. Name focus and selection
  indicators in `tw-blazor-css-strategy` rule 9.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
