# Round 1 — merged findings
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
- Description: The Design System page description still says "TimeWarp Enterprises brand." `PageTitleBar` renders it under the title. A configuration rebrand and `sourceName` both leave it, and template-smoke does not read `.razor` files. `## Results` says template copy no longer names TimeWarp Enterprises.
- Suggestion: Rewrite the description so it does not name TimeWarp Enterprises. Fail a check that scans `web-spa` `.razor` markup for `TimeWarp Enterprises`.
- Source: general
- Disposition notes: Fixed. Description now reads "Live brand tokens, type scale, and components for the configured brand." New `tests/container-apps/web/web-spa-integration-tests/configuration/brand-copy-tests.cs` (`BrandCopy_Should_`) fails on "TimeWarp Enterprises" in any web project `.razor` file.

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-blazor-css-strategy/SKILL.md:70
- Description: Rule 8 still says `App.razor` renders `data-theme-color="#55409c"`. `App.razor` now writes `Brand.NormalizedSeedColor`.
- Suggestion: Describe the seed as `BrandOptions.NormalizedSeedColor` and point at `tw-brand`.
- Source: general
- Disposition notes: Fixed. Rule 8 now names `BrandOptions.NormalizedSeedColor` and points at `tw-brand`.

## Duplicates / conflicts

- None. One reviewer.
