# Add BrandOptions white-label theming tokens logo Fluent ramp

## Description

A generated app should be able to wear another brand without editing the shell. `BrandOptions` is the configuration section for the product name, the logo, the shell design tokens, and the FluentUI ramp. The checked-in values stay the TimeWarp purple lockup, so the default app looks the same. Generated code carries no client name.

## Goal (implementer restatement)

A generated app wears another brand from configuration. The shell does not bake a client name, a logo, or a color into the page.

`BrandOptions` is the section on both hosts. The server configuration paints the first HTML: the document title, the body theme color, the logo, and `/brand.css`. The WebAssembly host binds the same section from its own appsettings, so a later interactive render keeps the same mark. When the section is missing, the code defaults are the checked-in TimeWarp brand.

The product name is the document title, the home page heading, and the home card title. The app bar and the focused auth page each author their own logo image from the configured source, alt text, width, and height. Those width and height attributes reserve the box. The painted height is `--twe-logo-height`, which `/brand.css` sets from `LogoHeight`. `tokens.css` keeps the same 48px fallback.

Shell colors stay custom properties in `tokens.css`. `/brand.css` is a linked stylesheet after `tokens.css` and before `app.css`. It restates the brand properties from `BrandOptions.Tokens` and the logo height. There is no `<style>` element and no inline style. A color that is not `#RGB` or `#RRGGBB` is rejected at startup and is not written into the sheet. Every declaration the default sheet emits is present in `tokens.css`. The two checked-in `BrandOptions` sections bind equal to each other and to `new BrandOptions()`.

The Fluent ramp is the seed on `<body data-theme-color>`. `ExactRamp` chooses the exact seed step or the harmonized ramp. Fluent does not read that flag from the body, so a classic script before `blazor.web.js` writes `color`, `exact`, and `base` into Fluent's theme storage and leaves a stored light or dark mode alone. The default is harmonized. The shell does not call `IThemeService.SetThemeAsync`.

`/brand.css` is anonymous. The response sends `Cache-Control: no-cache` and a strong ETag of the sheet. `If-None-Match` uses weak comparison: `*`, the same strong tag, a list that contains that tag, and that tag with a leading `W/` are 304 with no body. A different tag is 200. An absolute http(s) logo may include a query or a fragment. A relative or root-relative logo may not.

The style guide shows the configured name, the normalized seed, the ramp mode, and the logo. `ApplicationState` does not keep a second name or logo. Render tests pin the logo attributes. Pure tests pin the stylesheet, the startup script, the validator, the token drift, the two appsettings copies, and the cache validator.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` has no case-insensitive client-name match. Round-1 findings M1 through M7 and round-2 finding M8 are fixed on this branch.

## Checklist

- [x] `BrandOptions` plus token set, validator, and startup binding on the SPA host
- [x] `/brand.css` overrides the shell tokens and the logo height; `tokens.css` keeps the same defaults
- [x] App bar and focused page render the configured logo; home title and document title use the product name
- [x] Body seed color and the pre-Blazor script set the Fluent ramp, preserving stored mode
- [x] Style-guide brand card. HtmlRenderer logo tests. Stylesheet, script, and validator tests
- [x] `./bin/dev build` clean. `./bin/dev template-smoke` passes. `ganda repo audit` passes. No client name in the source or test diff
- [x] Review round 1 findings M1–M7 implemented on this branch. The ledger stays open for review round 2
- [x] Review round 2 finding M8: `If-None-Match` uses weak comparison, so a `W/` tag is 304

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: 2026-10-24 (implementer walk)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T21:53:51Z
- Review oracle round 1: claude (claude-opus-5-5), session ea832021-9579-4f4a-b67f-01532e447553, effort 3, general axis. An independent general subagent (a94ae757bf634b8e4) did a second pass. 2026-10-24
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-23T21:59:55Z
- Implement-fix-2 oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, 2026-10-24. Round-1 findings M1 through M7 fixed on this branch. The ledger was left open.
- Review oracle round 2: claude (claude-opus-5-5), session not reported, effort 2, general axis, max-turns 120, 2026-10-24. Reviewed the fix delta 7f9e21a90. Re-ran `BrandOptions_Should_` 7/7, `Clone_Should` 4/4, `TimeWarpPage_Should_` 5/5, and `./bin/dev build` (0 warnings, 0 errors).
- Implement-fix-3 oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, 2026-10-24. Round-2 finding M8 fixed on this branch. The ledger was left open for review round 3.
- Review oracle round 3: claude (claude-opus-5-5), session not reported, effort 1, general axis, max-turns 80, 2026-10-24. Reviewed the fix delta 5aa2a680e. Re-ran `BrandOptions_Should_` 7/7 and `./bin/dev build` (0 warnings, 0 errors). Disposition clean.

## Results

`BrandOptions` is configuration on both hosts. The checked-in section matches the code defaults (TimeWarp name, horizontal logo, purple tokens, seed `#55409c`, harmonized ramp). `/brand.css` is an anonymous GET mapped before static assets. It sends `Cache-Control: no-cache` and a strong ETag. `If-None-Match` uses weak comparison: the same tag, a list that contains it, `*`, and that tag with a leading `W/` are 304 with no body. The app bar, the focused page, and the style-guide brand card author their own logo image from those options. The home card title is `Welcome to ` plus `ProductName`. A script before `blazor.web.js` writes Fluent's stored `color`, `exact`, and `base`, and leaves `mode` alone. The shell does not call `IThemeService.SetThemeAsync`. `ApplicationState` does not keep a second name or logo. The style guide shows `NormalizedSeedColor`. An absolute http(s) logo may carry a query or a fragment. A relative or root-relative logo may not.

The signed-in Style Guide page was not opened in a browser. The first-paint Playwright run is the browser check for the shell: prerender HTML, `/brand.css` headers, a 304 for the strong tag and for the same tag with a leading `W/`, logo box, header color, and the settled Fluent brand.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class BrandOptions_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class TimeWarpPage_Should_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release -- --filter-class FirstPaint_Given_PrerenderedShell
```

**Expect:** `BrandOptions_Should_` 7 passed, 0 failed. The checked-in brand validates. Seed `red`, a `javascript:` logo, a protocol-relative logo, a blank product name, logo height 0, and a token of `purple` do not. An https logo, a root-relative logo, and seed ` #abc ` do. `https://cdn.example/mark.svg?v=1`, `https://cdn.example/mark.svg#mark`, and `https://cdn.example/mark.svg?v=1#mark` do. `/images/mark.svg?v=1` and `images/mark.svg#mark` do not. Binding `Northwind`, `#abc`, `ExactRamp` true, and `Tokens:Purple` `#123456` leaves `Tokens.Blue` at `#0085b2` and the logo source at the default. Every declaration from `BrandStylesheet.Render(new BrandOptions())` is in `tokens.css`, and the count is the `BrandTokenOptions` property count plus the logo height. A purple of `#abcdef` and a logo height of 64 appear in the sheet, and a purple of `nope` throws. The startup script sets `existing.color = "#112233";` and `existing.exact = true;` for that brand, names the Fluent storage key, sets `existing.base = "brand";`, and does not contain the product name, the word `mode`, or `<`. The default script uses `#55409c` and `existing.exact = false;`. A seed of `red` throws. Both checked-in `BrandOptions` sections bind equal to each other and to `new BrandOptions()`. The default sheet returns 200 with a quoted strong ETag. The same tag, a list that contains it, `*`, the tag with a leading `W/`, and a list that contains that weak tag return 304 with no body. `W/"other"` stays 200. A different purple changes the tag. `TimeWarpPage_Should_` 5 passed, 0 failed. The app bar and the focused page render `data-qa="BrandLogo"` with the configured source, alt, width, and height (default `images/timewarp-horizontal.svg`, 201 by 48). The home card title stays `Welcome to TimeWarp.Architecture`. `FirstPaint_Given_PrerenderedShell` 2 passed, 0 failed. Prerender HTML contains `href="brand.css"`, `images/timewarp-horizontal.svg`, and `data-theme-color="#55409c"`. GET `/brand.css` is OK, contains `--twe-purple: #55409c;` and `--twe-logo-height: 48px;`, sends `cache-control: no-cache`, and a quoted ETag. A repeat with `If-None-Match` set to that tag is 304. A repeat with `W/` plus that tag is 304. With JavaScript off, before Blazor, and after settle, the logo is 201 by 48 and the header background is `rgb(85, 64, 156)`. The settled Fluent brand is `#6b55a9`.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit || echo NO_CLIENT_NAME
```

`dev build`: 0 warnings, 0 errors. `dev template-smoke`: `Template smoke SUCCEEDED`. SmokeDefault OK. create-role 5/5, weather 5/5, hello 2/2, web-jaribu-tests 405/405, api-jaribu-tests 9/9, timewarp-testing-tests 14/14. `ganda repo audit`: Passed 33, Failed 0, Skipped 1 (self-hosted, public repository). The client-name search prints `NO_CLIENT_NAME`.

### Review

Round 1 found M1 through M7 and left them open. This fix pass implements them on the task branch. The ledger is unchanged so review round 2 can close them.

- M1: the drift test checks every default declaration against `tokens.css`.
- M2: both appsettings sections bind equal to `new BrandOptions()`.
- M3: the home card title is built from `ProductName`.
- M4: `ApplicationState.Name` and `Logo` are removed.
- M5: the style guide shows `NormalizedSeedColor`.
- M6: query and fragment are allowed only on absolute http(s) logos.
- M7: `/brand.css` sends `Cache-Control: no-cache` and a strong ETag, and a match is 304.

Artifacts:

- Framework: `review/review-framework.md`
- Findings: `review/round-1/general.md`
- Round 1 ledger (frozen): `review/round-1/merged.md`
- Round 2 (frozen): `review/round-2/general.md`, `review/round-2/merged.md`
- Round 3: `review/round-3/general.md`. Final ledger: `review/round-3/merged.md`
- Disposition: `review/disposition.md`

Round 2 (effort 2, general) confirms M1 through M7 are fixed. It opens M8 (suggestion): `BrandStylesheet` compares `If-None-Match` strongly, and the test pins `W/` as 200. RFC 9110 requires weak comparison there. Counts at the end of round 2: bug 0 open / 2 fixed, suggestion 1 open / 2 fixed, nit 0 open / 3 fixed.

This fix pass implements M8. The ledger is unchanged so review round 3 can close it.

- M8: a listed `If-None-Match` tag matches after a leading `W/` is removed. The response tag stays strong. `W/"other"` stays 200.

Round 3 (effort 1, general) confirms M8 is fixed in 5aa2a680e and raises no new findings. It re-ran `BrandOptions_Should_` 7/7 and `./bin/dev build` (0 warnings, 0 errors).

**Review disposition: clean.** 3 rounds. Roster: general at effort 3, then 2, then 1. Final counts: bug 0 open / 2 fixed / 0 wontfix, suggestion 0 open / 3 fixed / 0 wontfix, nit 0 open / 3 fixed / 0 wontfix. No wontfix and no escalation.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7
- Round 2: Blocked — open M8
- Round 3: Done
## Notes

- Source idea (read-only): the portal's `BrandTheme` seed and exact-versus-harmonized `ThemeSettings`. This template does not copy that orange, that product name, or a settings-page toggle. The operator sets the brand in configuration. The shell does not call `IThemeService.SetThemeAsync` (task 290: that painted Fluent's default blue first).
- Out of scope: shell slots (379-015), sign-in options (379-016), and the StyleGuidePage contract (379-017).
- SiteMetadata stays the link-preview card. It is not the shell logo.
