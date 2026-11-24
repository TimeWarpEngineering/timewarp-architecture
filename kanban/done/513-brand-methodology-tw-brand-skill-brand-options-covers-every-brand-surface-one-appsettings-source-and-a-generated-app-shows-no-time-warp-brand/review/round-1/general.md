# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/513-brand-methodology-tw-brand-skill-brand-options` vs merge-base with master. Brand options, stylesheet, document, manifest, endpoints, SPA loader, tokens and component CSS, template smoke, skills, and the recorded `## Results`.

## Summary

The server-owned `BrandOptions` path is in place: the SPA copy is gone, `/brand.json` round-trips every writable property, `/brand.css` writes the role sheet and a dark text block, the manifest is served, component CSS under `features/` and `components/` no longer reads the hue palette, and the recorded gates name the suites that cover that work. The rebrand still leaves "TimeWarp Enterprises brand" on the Design System page, which is the page this task screenshots, and `tw-blazor-css-strategy` rule 8 still describes a hardcoded seed that `App.razor` no longer renders.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/design-system/pages/DesignSystemPage.razor:56
- Description: The page description rendered under the Design System title is "Live brand tokens, type scale, and components — TimeWarp Enterprises brand." `PageTitleBar` puts that fragment in `twe-page__description`, so it is visible copy. `sourceName` rewrites `TimeWarp.Architecture`, not this phrase, and a `BrandOptions` edit does not touch it. The home page, `index.md`, the agent cards, `auth.md`, and `SiteMetadata` were cleared of "TimeWarp Enterprises"; this page was not. `## Results` says template copy no longer names TimeWarp Enterprises. Template-smoke searches `wwwroot` and `App.razor` only, so the sentence stays green.
- Suggestion: Rewrite the description so it does not name TimeWarp Enterprises (the Brand card already says the configured brand). Fail a check that scans `web-spa` `.razor` markup for `TimeWarp Enterprises`, so the next leftover is not outside the wwwroot grep.
- Status: open

### Issue 2 — Severity: suggestion
- File: skills/tw-blazor-css-strategy/SKILL.md:70
- Description: Rule 8 still says `App.razor` renders `<body data-theme-color="#55409c" data-theme="system">`. This diff changed `App.razor` to `data-theme-color="@Brand.NormalizedSeedColor"`. The same skill's later paragraph correctly points brand colors at `BrandOptions`. An agent following rule 8 will treat the seed as a hardcoded TimeWarp purple.
- Suggestion: State that the seed attribute is `BrandOptions.NormalizedSeedColor`, and point at `tw-brand` for the value.
- Status: open
