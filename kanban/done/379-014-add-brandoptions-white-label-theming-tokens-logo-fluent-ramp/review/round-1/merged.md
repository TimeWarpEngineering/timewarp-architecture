# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general (review oracle pass and independent subagent pass, merged in `general.md`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/configuration/brand-options-tests.cs:76
- Description: The 19 default values in `BrandTokenOptions` restate `tokens.css`. The drift test checks only purple and the logo height, although its Design region says it prevents drift. This breaks the AGENTS.md hard rule "generate one from the other or add a build check".
- Suggestion: Check every declaration from `BrandStylesheet.Render(new BrandOptions())` against `tokens.css`.
- Source: general (both passes)
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/wwwroot/appsettings.json:14
- Description: The `BrandOptions` section is copied into the server and WebAssembly appsettings. Only a comment keeps the copies in step. If they drift, the shell switches marks during hydration. This breaks the same hard rule.
- Suggestion: Add a test that binds both files and asserts they are equal to each other and to `new BrandOptions()`, or generate one file from the other.
- Source: general (both passes)
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:18
- Description: The home card title is still the fixed text `Welcome to TimeWarp.Architecture`.
- Suggestion: Build the title from `ProductName`, or make it neutral.
- Source: general (both passes)
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.cs:45
- Description: `ApplicationState.Name` and `Logo` are now unread outside the debug action. Under a custom brand they hold a second name and logo that disagree with `BrandOptions`.
- Suggestion: Remove them, or label them debug-only.
- Source: general (oracle)
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:96
- Description: The style guide shows the raw `SeedColor`, not `NormalizedSeedColor`.
- Suggestion: Use `NormalizedSeedColor`.
- Source: general (subagent)
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/configuration/brand-options.cs:102
- Description: Logo URLs that contain `?` or `#` are rejected, which blocks cache-busted CDN URLs.
- Suggestion: Allow them in absolute http(s) URLs, or record the reason in the Design region.
- Source: general (subagent)
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-server/program.cs:573
- Description: `/brand.css` blocks rendering and is sent with no caching or validation headers.
- Suggestion: Send `Cache-Control: no-cache` plus an ETag.
- Source: general (subagent)
- Disposition notes:

## Duplicates / conflicts

- Both passes raised the `tokens.css` drift test and the duplicated appsettings sections. The subagent rated them suggestions. The oracle rated them bugs, because each one breaks an AGENTS.md hard rule and the test's Design region claims a guard that does not exist. The stronger severity is kept.
- The subagent also raised two points that are not listed. First, `SiteMetadata` still carries the TimeWarp name for link previews. That is out of scope, because the task Notes say SiteMetadata stays the link-preview card. Second, the style-guide colour swatches use fixed hex labels. That code predates this change and its labels document `tokens.css`. The subagent also suggested zeroing Fluent's stored `hue` and `vibrancy`. Nothing in the repo writes those values, so that suggestion is not listed either.
- No misread: the implementer's Goal restatement matches the task Description.
