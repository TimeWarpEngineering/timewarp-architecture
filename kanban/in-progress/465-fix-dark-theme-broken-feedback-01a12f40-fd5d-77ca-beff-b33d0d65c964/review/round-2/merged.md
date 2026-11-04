# Round 2 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor.css:35
  (also `SegmentedControl`, `PeriodStepper`, `FilterSelect`, `FilterBar`, `AskReferencePicker`,
  `AgentAsk`, `wwwroot/css/app.css:113`)
- Description: Dark-mode focus rings and focused-field borders read `--twe-primary`, about 1.6:1 on
  dark paper.
- Source: general (round 1)
- Disposition notes: Fixed in `016def93c`. The new `--twe-focus-ring` role token is
  `var(--twe-primary)` in light (unchanged) and `var(--twe-purple-soft)` in dark (about 7.1:1). All
  listed rules read it. The active-tab underline reads `--twe-primary-text`. Skill rule 9 names focus
  and selection indicators. `DarkTheme_Given_Shell` asserts 3:1. Re-verified in round 2: build
  0 warnings, the dark test passed 2/2, lint-skills passed.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
