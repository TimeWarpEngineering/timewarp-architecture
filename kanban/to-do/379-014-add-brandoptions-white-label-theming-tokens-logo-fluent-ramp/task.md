# Add BrandOptions white-label theming tokens logo Fluent ramp

## Description

A generated app should be able to wear another brand without editing the shell. `BrandOptions` is the configuration section for the product name, the logo, the shell design tokens, and the FluentUI ramp. The checked-in values stay the TimeWarp purple lockup, so the default app looks the same. Generated code carries no client name.

## Goal (implementer restatement)

The shell's brand is configuration, not a color and a logo baked into the page.

`BrandOptions` binds from the section named `BrandOptions` on both hosts. The server host renders the first HTML, so its configuration supplies the document title, the body theme color, the logo in the prerendered shell, and `/brand.css`. The WebAssembly host reads the same section from its own appsettings so a later interactive render does not snap back to a different mark. Code defaults match the checked-in TimeWarp brand when the section is absent.

The product name is the document title and the home page heading. The logo is an image the two shells author themselves (the app bar and the focused auth page), with source, alt text, width, and height from the options. The width and height attributes reserve the box. The painted height is `--twe-logo-height`, which `/brand.css` sets from `LogoHeight`, and `tokens.css` defines the same 48px fallback.

Shell colors stay custom properties in `tokens.css`. `/brand.css` is a real stylesheet linked after `tokens.css` and before `app.css`. It restates the brand properties from `BrandOptions.Tokens` (purple, blue, green, primary, secondary, highlight, info, and the logo height). There is no `<style>` element and no inline style. A hex that is not `#RGB` or `#RRGGBB` is rejected at startup and is not written into the sheet.

The Fluent ramp is the seed color on `<body data-theme-color>`, which Fluent's startup script already reads before any component is interactive. `ExactRamp` chooses the exact seed step versus the harmonized ramp. Fluent does not read that flag from the body, so a classic script placed before `blazor.web.js` writes `color`, `exact`, and `base` into Fluent's theme storage and leaves a stored light or dark mode alone. The default is harmonized, which is the ramp the app already paints.

The style guide shows the configured name, seed, ramp mode, and logo. The page contract is unchanged. Render tests pin the logo attributes. A pure test pins the stylesheet, the startup script, and the validator.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` has no case-insensitive client-name match.

## Checklist

- [x] `BrandOptions` plus token set, validator, and startup binding on the SPA host
- [x] `/brand.css` overrides the shell tokens and the logo height; `tokens.css` keeps the same defaults
- [x] App bar and focused page render the configured logo; home title and document title use the product name
- [x] Body seed color and the pre-Blazor script set the Fluent ramp, preserving stored mode
- [x] Style-guide brand card. HtmlRenderer logo tests. Stylesheet, script, and validator tests
- [x] `./bin/dev build` clean. `./bin/dev template-smoke` passes. `ganda repo audit` passes. No client name in the source or test diff

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: 2026-10-24 (implementer walk)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T21:53:51Z
- Review oracle round 1: claude (claude-opus-5-5), session ea832021-9579-4f4a-b67f-01532e447553, effort 3, general axis. An independent general subagent (a94ae757bf634b8e4) did a second pass. 2026-10-24

## Results

`BrandOptions` is configuration on both hosts. The checked-in section matches the code defaults (TimeWarp name, horizontal logo, purple tokens, seed `#55409c`, harmonized ramp). `/brand.css` is an anonymous GET mapped before static assets. The app bar, the focused page, and the style-guide brand card author their own logo image from those options. A script before `blazor.web.js` writes Fluent's stored `color`, `exact`, and `base`, and leaves `mode` alone. The shell does not call `IThemeService.SetThemeAsync`.

The signed-in Style Guide page was not opened in a browser. The first-paint Playwright run is the browser check for the shell: prerender HTML, `/brand.css`, logo box, header color, and the settled Fluent brand.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class BrandOptions_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class TimeWarpPage_Should_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release -- --filter-class FirstPaint_Given_PrerenderedShell
```

**Expect:** `BrandOptions_Should_` 5 passed, 0 failed. The checked-in brand validates. Seed `red`, a `javascript:` logo, a protocol-relative logo, a blank product name, logo height 0, and a token of `purple` do not. An https logo, a root-relative logo, and seed ` #abc ` do. Binding `Northwind`, `#abc`, `ExactRamp` true, and `Tokens:Purple` `#123456` leaves `Tokens.Blue` at `#0085b2` and the logo source at the default. `/brand.css` for the defaults contains `--twe-purple: #55409c;`, `--twe-logo-height: 48px;`, and no `<`. `tokens.css` contains the same two declarations. A purple of `#abcdef` and a logo height of 64 appear in the sheet, and a purple of `nope` throws. The startup script sets `existing.color = "#112233";` and `existing.exact = true;` for that brand, names the Fluent storage key, sets `existing.base = "brand";`, and does not contain the product name, the word `mode`, or `<`. The default script uses `#55409c` and `existing.exact = false;`. A seed of `red` throws. `TimeWarpPage_Should_` 5 passed, 0 failed. The app bar and the focused page render `data-qa="BrandLogo"` with the configured source, alt, width, and height (default `images/timewarp-horizontal.svg`, 201 by 48). `FirstPaint_Given_PrerenderedShell` 2 passed, 0 failed. Prerender HTML contains `href="brand.css"`, `images/timewarp-horizontal.svg`, and `data-theme-color="#55409c"`. GET `/brand.css` is OK and contains `--twe-purple: #55409c;` and `--twe-logo-height: 48px;`. With JavaScript off, before Blazor, and after settle, the logo is 201 by 48 and the header background is `rgb(85, 64, 156)`. The settled Fluent brand is `#6b55a9`.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit || echo NO_CLIENT_NAME
```

`dev build`: 0 warnings, 0 errors (00:00:05.84). `dev template-smoke`: `Template smoke SUCCEEDED`. SmokeDefault OK. create-role 5/5, weather 5/5, hello 2/2, web-jaribu-tests 405/405, api-jaribu-tests 9/9, timewarp-testing-tests 14/14. `ganda repo audit`: Passed 33, Failed 0, Skipped 1 (self-hosted, public repository). The client-name search prints `NO_CLIENT_NAME`.

### Review

Round 1 ran at effort 3 with the general roster: the oracle pass plus an independent subagent pass. It found 2 bugs, 2 suggestions, and 3 nits, all open. There is no disposition yet, and a fix pass is needed.

- M1 (bug): the `tokens.css` drift test checks 2 of the 19 default token values.
- M2 (bug): the duplicated `BrandOptions` appsettings sections have no equality check.

Both bugs break the AGENTS.md rule "generate one from the other or add a build check". Re-run by the reviewer: `BrandOptions_Should_` passed 5 of 5, and `TimeWarpPage_Should_` passed 5 of 5.

Artifacts:

- Framework: `review/review-framework.md`
- Findings: `review/round-1/general.md`
- Live ledger: `review/round-1/merged.md`

## Notes

- Source idea (read-only): the portal's `BrandTheme` seed and exact-versus-harmonized `ThemeSettings`. This template does not copy that orange, that product name, or a settings-page toggle. The operator sets the brand in configuration. The shell does not call `IThemeService.SetThemeAsync` (task 290: that painted Fluent's default blue first).
- Out of scope: shell slots (379-015), sign-in options (379-016), and the StyleGuidePage contract (379-017).
- SiteMetadata stays the link-preview card. It is not the shell logo.
