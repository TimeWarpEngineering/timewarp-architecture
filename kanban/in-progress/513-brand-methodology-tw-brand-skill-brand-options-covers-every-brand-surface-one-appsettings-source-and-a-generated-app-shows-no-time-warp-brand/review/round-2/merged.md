# Round 2 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/design-system/pages/DesignSystemPage.razor:56
- Description: The Design System page description still said "TimeWarp Enterprises brand." A configuration rebrand and `sourceName` both left it, and template-smoke did not read `.razor` files.
- Suggestion: Rewrite the description so it does not name TimeWarp Enterprises. Fail a check that scans web-project `.razor` markup for `TimeWarp Enterprises`.
- Source: general
- Disposition notes: Fixed in `27f8dc2fd`. The description reads "Live brand tokens, type scale, and components for the configured brand." `BrandCopy_Should_` fails when any `.razor` file under `source/container-apps/web/projects` contains "TimeWarp Enterprises". Round 2 re-read the page and the test, and a `*.razor` search found no remaining hit. Recorded fix-pass result: `BrandCopy_Should_` 1/1, `DesignSystemPage_Should_` 6/6.

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-blazor-css-strategy/SKILL.md:70
- Description: Rule 8 still said `App.razor` renders `data-theme-color="#55409c"`. `App.razor` writes `Brand.NormalizedSeedColor`.
- Suggestion: Describe the seed as `BrandOptions.NormalizedSeedColor` and point at `tw-brand`.
- Source: general
- Disposition notes: Fixed in `27f8dc2fd`. Rule 8 names `BrandOptions.NormalizedSeedColor` and points at `tw-brand`. `App.razor:67` is `data-theme-color="@Brand.NormalizedSeedColor"`. Recorded fix-pass result: `./bin/dev lint-skills` passed.

## Duplicates / conflicts

- No new findings. M1 and M2 are carried from round 1 with status re-checked on the fix delta.
