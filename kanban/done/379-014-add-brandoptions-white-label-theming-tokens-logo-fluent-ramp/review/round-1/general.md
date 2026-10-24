# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** commit 305c57984 against f34ccbe4c, `source/` and `tests/`. This file merges two independent general passes: the review oracle, and a subagent that did not see the oracle's conclusions.

## Summary

The change moves the shell brand into `BrandOptions` on both hosts. `/brand.css` is a real linked sheet, the logo comes from options in both shells, the seed goes on `<body data-theme-color>`, and a classic script before `blazor.web.js` writes Fluent's `color`, `exact`, and `base` while leaving `mode` alone. The safety work is solid. Hex values are checked again when the CSS and the script are rendered, logo URLs reject `javascript:`, protocol-relative, and `..` forms, and the product name and alt text go through Razor encoding. Purpose and Design regions are present, file names are kebab-case, and no `#if` flag region was touched. There is no `<style>` element, no `SetThemeAsync` call, and no client name.

The main problem is the repo hard rule "When two things must agree, generate one from the other or add a build check." This change adds two pairs of values that must agree, and nothing checks either pair. The rest are small white-label gaps.

Re-verified by the oracle: `BrandOptions_Should_` passed 5 of 5, and `TimeWarpPage_Should_` passed 5 of 5 (Release, local). Build, template-smoke, and audit were not re-run in this round.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/configuration/brand-options-tests.cs:76
- Description: `BrandTokenOptions` restates 19 `tokens.css` hex values as C# defaults, and `tokens.css` is the fallback until `/brand.css` arrives. The test's Design region says `tokens.css` "is read from the repo so a default hex cannot drift from the fallback sheet". The test checks only `--twe-purple` and `--twe-logo-height`. The other 18 defaults can drift without a failure, so a default app would paint one blue from `tokens.css` and another from `/brand.css`. That breaks the AGENTS.md hard rule, and the Design region claims a guard that does not exist.
- Suggestion: Render `BrandStylesheet.Render(new BrandOptions())`, parse every `--twe-*: value;` line, and assert that `tokens.css` contains each identical declaration. A loop over the rendered lines covers tokens added later too.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/wwwroot/appsettings.json:14
- Description: The full `BrandOptions` section is copied into `web-server/appsettings.json:42` and `web-spa/wwwroot/appsettings.json:14`. The Goal says the WebAssembly copy exists "so a later interactive render does not snap back to a different mark". Only a `_comment` keeps the two copies in step. If an operator edits the server copy only, the prerendered logo and home title change, and then switch back to TimeWarp after hydration, which is the swap this design is meant to prevent. This is the second break of the "generate one from the other or add a build check" rule.
- Suggestion: Add a test that binds `BrandOptions` from both JSON files and asserts the results are equal, including every token. Also assert that the checked-in copy equals `new BrandOptions()`, which the Goal promises. Generating one file from the other is fine too. Either way, the rule then has a check.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:18
- Description: The page title now uses `ProductName`, but the first card on the home page still says `Welcome to TimeWarp.Architecture`. A white-labelled app still greets its users with TimeWarp on its landing page.
- Suggestion: Use `$"Welcome to {BrandOptionsAccessor.Value.ProductName}"`, or a neutral title.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.cs:45
- Description: `ApplicationState.Name` and `ApplicationState.Logo` are no longer read by any page or shell. They default to the TimeWarp name and `/images/logo.png`. The only writer left is the debug action. Under a custom brand, the store still holds a second product name and logo that disagree with `BrandOptions`, and the new Design note ("a store that has only run Initialize agrees with the shell") is only true for the default brand.
- Suggestion: Remove `Name` and `Logo` from the state and its debug action, together with their tests if any. If they must stay, describe them as debug-only and do not imply that they follow the brand.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:96
- Description: The brand card shows the raw `@Brand.SeedColor`. The body, script, and sheet use `NormalizedSeedColor`, so a configured ` #abc ` shows with spaces here and nowhere else.
- Suggestion: Show `Brand.NormalizedSeedColor`.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/configuration/brand-options.cs:102
- Description: `IsLogoReference` rejects `?` and `#`. That blocks common cache-busted CDN URLs such as `https://cdn.example/mark.svg?v=3`. Razor encodes the attribute, so these characters carry no injection risk, and the other checks already cover quotes and angle brackets.
- Suggestion: Allow `?` and `#` in absolute http(s) URLs, or record why they are rejected in the Design region.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-server/program.cs:573
- Description: `/brand.css` is a render-blocking stylesheet sent with no `Cache-Control`, `ETag`, or `Last-Modified`, so every full page load downloads it again before paint.
- Suggestion: Send `Cache-Control: no-cache` with an ETag computed from the rendered text, so the browser can revalidate cheaply and still pick up a configuration change on the next load.
- Status: open
