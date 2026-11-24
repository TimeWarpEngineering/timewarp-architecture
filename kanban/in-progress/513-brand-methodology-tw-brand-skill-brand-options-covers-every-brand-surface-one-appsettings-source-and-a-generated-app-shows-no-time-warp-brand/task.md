# Brand methodology: tw-brand skill, BrandOptions covers every brand surface, one appsettings source, and a generated app shows no TimeWarp brand

## Description

The first real app from this template launches soon and will change colours, logo, and maybe
the assistant name. The intended model (task 379-014) is sound: a generated app wears its brand
through `BrandOptions` configuration (product name, logo, seed colour, token overrides served as
`/brand.css`), and the assistant name is a runtime admin setting on the AI page. Nothing is a
`dotnet new` parameter beyond the implicit `sourceName` rewrite. That is the methodology to keep:
**visual brand is set once by the app's agent in configuration; identity words an admin can
change at runtime; nothing requires editing shell code.**

The 2026-11-24 inventory found the model leaks:

- `BrandOptions` is duplicated in `web-server/appsettings.json` and
  `web-spa/wwwroot/appsettings.json`, and `BrandOptions_Should_` pins both to the code defaults, so
  a rebrand edits three places and a test.
- `tokens.css` never overrides brand keys in dark mode; `--twe-purple-soft` and
  `--twe-blue-bright` carry dark text and must be hand-tuned per palette.
- Favicons, touch and PWA icons, `images/og-image.png`, `images/logo.png`, `manifest.json`
  `theme_color`/`background_color`, and `LogoAlt` ("TimeWarp Architecture", not rewritten by
  `sourceName`) are hard files and literals outside `BrandOptions`. `images/timewarp/*` and
  `images/the-freeze-team/*` ship in every app.
- The brand tokens are named by hue (`--twe-purple`, `--twe-blue`, `--twe-green`, 35 CSS files
  reference them) and the `/DesignSystem` Color Tokens card labels them "Purple (primary)",
  "Blue (secondary)", "Green (highlight)". The swatches paint from `var(--twe-*)`, so `/brand.css`
  does recolour them, but a rebranded app shows an orange chip labelled Purple. Role tokens
  (`--twe-primary`, `--twe-secondary`, `--twe-highlight`) already exist beside them.
- No skill or document states the procedure.

## Depends on

- 506

## Requirements

### `tw-brand` skill

- New `skills/tw-brand/SKILL.md`: the methodology sentence above, the exact steps an agent runs
  to rebrand a generated app (the files, the `BrandOptions` keys, how to derive dark variants, how
  to generate the icon set from one SVG, how to verify), and what is admin-runtime (assistant
  name; product display name if this task adds it). Register it in `AGENTS.md` "Where to look".
  `dev lint-skills` green.

### One source, full coverage

- One `BrandOptions` source. The server owns it and the SPA reads it from the server (an
  anonymous endpoint beside `/brand.css`, or the server writes the SPA's copy at build). Delete the
  equality test with the duplicate.
- `BrandOptions` gains: `LogoAlt` defaulting from `ProductName`; icon set (`FaviconSrc`,
  `AppleTouchIconSrc`, `Icon192Src`, `Icon512Src`, `OgImageSrc`); manifest `ThemeColor` and
  `BackgroundColor` defaulting from the seed and `--twe-background`; dark-mode variants for the
  role tokens (`PrimaryDark`, `SecondaryDark`, `HighlightDark`, or a documented derivation).
  `App.razor` `<link>` tags and `manifest.json` (served, not static) read them. Startup validation
  covers the new values.
- `/brand.css` emits the dark-mode block for the role tokens.
- Remove `images/timewarp/*` and `images/the-freeze-team/*` from template content if nothing
  references them, or move them under a path the template excludes.

### Tokens named by role

- Product CSS references role tokens only: `--twe-primary*`, `--twe-secondary*`,
  `--twe-highlight`, and their dark variants. Hue-named tokens (`--twe-purple*`, `--twe-blue*`,
  `--twe-green*`) remain in `tokens.css` as the TimeWarp palette that seeds the roles, but no
  component CSS reads them (35 files today). The Color Tokens card lists roles ("Primary",
  "Secondary", "Highlight"), and the Brand card already shows the configured brand.
- A test or `dev` check fails on a new `var(--twe-purple` / `--twe-blue` / `--twe-green` reference
  under `features/` or `components/`.

### Proof

- `dev template-smoke` gains an assertion: generate with a sample `BrandOptions` (different name,
  logo, seed, icons) and grep the output's `wwwroot`, `App.razor`, `manifest.json`, `llms.txt`,
  and served `/brand.css` for `TimeWarp`, `timewarp`, `#55409c`, and the TimeWarp SVG filenames.
  Zero hits.
- Screenshots in `evidence/` of the rebranded sample app: header, `/DesignSystem` Brand and
  Color Tokens cards, light and dark, and the browser tab icon.

## Out of scope

- Runtime admin upload of logos or colours. That is a multi-tenant feature nothing asks for yet.
  Assistant name stays the only runtime brand setting; add product display name as a second only
  if it falls out cheaply, and say so in Results.

## Goal (implementer restatement)

A generated app changes its whole visual brand by editing one `BrandOptions` section in
web-server configuration and dropping in its own image files. No shell code edit. The
assistant name stays the only runtime (admin) brand setting.

Acceptance, as I read it:

1. `skills/tw-brand/SKILL.md` states that rule and the exact rebrand steps (files, keys, dark
   variants, icon set from one SVG, verification). `AGENTS.md` "Where to look" names it.
   `dev lint-skills` is green.
2. The server owns `BrandOptions`. The SPA gets it from the server (`GET /brand.json`), the SPA
   `appsettings.json` copy and the test that pinned the two copies equal are gone.
3. `BrandOptions` covers every brand surface: `LogoAlt` falls back to `ProductName`; the icon set
   (`FaviconSrc`, `AppleTouchIconSrc`, `Icon192Src`, `Icon512Src`, `OgImageSrc`); manifest
   `ThemeColor` / `BackgroundColor` falling back to the seed and `--twe-background`; dark role
   variants `PrimaryDark` / `SecondaryDark` / `HighlightDark`. `App.razor` link tags and a served
   `/manifest.json` read them; startup validation covers them; `/brand.css` writes a dark block.
4. Unreferenced TimeWarp art (`images/timewarp/*`, `images/the-freeze-team/*`, and the other
   unreferenced icon copies) leaves template content.
5. Component CSS reads role tokens only. The hue palette stays in `tokens.css` and seeds the role
   defaults. A test fails on a new hue `var()` under `features/` or `components/`. The Color
   Tokens card shows Primary / Secondary / Highlight.
6. `dev template-smoke` generates an app, applies a sample rebrand by configuration and files
   only, and finds zero TimeWarp brand hits in the named surfaces. Served `/brand.css` and
   `/manifest.json` for a sample brand are proven brand-clean by an in-proc web-server test.
7. Screenshots of the rebranded app (header, Brand and Color Tokens cards, light and dark, tab
   icon) in `evidence/`.
8. Gates: `dev build` 0/0, touched tests, `dev lint-skills`, `dev template-smoke`, `ganda repo audit`.

## Checklist

- [x] `skills/tw-brand/SKILL.md` + AGENTS.md row; `dev lint-skills` green
- [x] Single `BrandOptions` source; duplicate test deleted
- [x] Icon set, manifest colours, `LogoAlt`, dark role variants in `BrandOptions`; `/brand.css` dark block
- [x] Component CSS on role tokens only; guard check in place; Color Tokens card lists roles
- [x] Template-smoke rebrand assertion passes with zero TimeWarp hits
- [x] Screenshots in `evidence/`
- [x] `dev build` 0/0, web-spa integration tests, `dev template-smoke`, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's branding question ahead of the first generated app.
  Inventory in this session: no surface is a template parameter; assistant name is the only
  runtime setting; 379-014 set the model.
- 2026-11-25 implement (Claude): server-owned BrandOptions with `/brand.json` for the SPA, served
  `/manifest.json`, role tokens plus dark text roles in `/brand.css`, component CSS moved off the
  hue palette, template-smoke rebrand check, `tw-brand` skill, evidence screenshots. Found while
  testing: the configuration binder writes a getter's value back into the property, so a
  fallback inside a bound getter (LogoAlt from ProductName) froze at bind time; fallbacks are
  `Effective*` read-only properties instead.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 263ce09f-6e17-490e-a6fc-8f7bdd898405, max-turns 500, Done — 2026-11-24T18:20:14Z
- Reviewer: grok session 01a20ec9-9a9a-7713-a38f-1427674d18bd (2026-11-25). Round 1, effort 3, roster general. Open findings in `review/round-1/merged.md`. Disposition not written.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20ec9-9a9a-7713-a38f-1427674d18bd, max-turns 200, artifacts general.md, merged.md — 2026-11-24T18:30:29Z

## Learnings

- No goal misread. The restatement matches the task: one server-owned `BrandOptions` section, full surface coverage, role tokens, the skill, the rebrand proof, and the screenshots. Served `/brand.css` is proved by the in-proc host test rather than a grep of a generated file; the restatement says that, because those documents are not files in the generated tree.

## Results

### What changed

- **One source.** `BrandOptions` lives only in `web-server/appsettings.json`. The SPA's
  `wwwroot/appsettings.json` copy is gone; WebAssembly `Main` reads `GET /brand.json`
  (`BrandDocument`) into its configuration before the container is built. The test that pinned
  the two copies equal is deleted; `Round_Trip_Every_Property_Through_The_Brand_Document` binds
  the document back with every property off its default, so a property `BrandDocument` forgets
  fails.
- **Coverage.** `BrandOptions` gained `FaviconSrc`, `AppleTouchIconSrc`, `Icon192Src`,
  `Icon512Src`, `OgImageSrc`, `ThemeColor`, `BackgroundColor`; `LogoAlt` is empty by default.
  Fallbacks are read-only `EffectiveLogoAlt` (ProductName), `EffectiveThemeColor` (seed),
  `EffectiveBackgroundColor` (`#fafafb`). `BrandTokenOptions` is role-named only: Primary,
  PrimaryHover, PrimaryActive, PrimarySoft, OnPrimary, Secondary, SecondaryHover,
  SecondaryActive, SecondaryBright, Highlight, HighlightSoft, Info, and the dark variants
  PrimaryDark, SecondaryDark, HighlightDark (the hue keys Purple…GreenSoft are removed).
  The validator covers every asset path and color.
- **Served documents.** `BrandEndpoints` (renamed from `BrandStylesheetEndpoint`) maps
  `/brand.css`, `/brand.json`, `/manifest.json`, anonymous, no-cache + strong ETag.
  `wwwroot/manifest.json` is deleted. `App.razor` icon links read `BrandOptions`. `/brand.css`
  writes `:root` roles and a `body[data-theme="dark"]` block with `--twe-primary-text`,
  `--twe-secondary-text`, `--twe-highlight-text` from the dark variants.
- **Link preview.** `SiteMetadata.ImagePath` is gone (the image is `BrandOptions.OgImageSrc`);
  `SiteName`, `Title`, `ImageAlt` are empty by default and fall back to the product name and
  logo alt.
- **Tokens by role.** Component CSS (10 files) reads role tokens only; `tokens.css` keeps the hue
  palette as the seed of the role defaults, derives links / accent text / focus ring from the
  text roles in both modes. Guard: `BrandRoleTokens_Should_` scans `features/` and `components/`
  of every web project. Color Tokens card lists Primary, Secondary, Highlight.
- **Template content.** Removed `images/timewarp/*`, `images/the-freeze-team/*`,
  `images/logo.png`, `images/timewarp-logo.svg`, `favicons/*`, `favicon.ico`, `favicon.png`,
  `favicon-16x16.png` (none referenced). Template copy no longer names TimeWarp Enterprises
  (home page, `index.md`, `agent.json`/`agent-card.json` provider, `auth.md` examples,
  `SiteMetadata` description). The `timewarp-architecture-discovery` agent skill is renamed
  `host-discovery`; both skill digests in `agent-skills/index.json` are recomputed (the
  `timewarp-identity` digest was stale). `appNameKebab` now also rewrites the MCP card
  `serverInfo.name`. The template excludes `web-spa/wwwroot/js/**` (gitignored TypeScript build
  output the pack picked up from disk).
- **Proof.** `TemplateSmokeBrandSurfaces` (template-smoke, on the generate-only Contoso.Shop
  output): replaces the `BrandOptions` section, adds a new logo, overwrites the icon set, deletes
  `timewarp-horizontal.svg`, then searches every `web-spa/wwwroot` file (content and name) and
  `App.razor` for `timewarp` (any case) and `#55409c`, and fails if `wwwroot/manifest.json`
  exists or the SPA has a `BrandOptions` copy. Allowed by name: TimeWarp library names
  (`TimeWarp.State`, `TimeWarp.Identity`, …, not `TimeWarp.Architecture`), `TimeWarp DevTools`,
  `TimeWarp*` code identifiers, library repo/docs URLs, the `timewarp-identity` agent skill,
  `x-timewarp-*` OpenAPI extensions, and `tokens.css` hue palette lines. Served `/brand.css`,
  `/manifest.json`, `/brand.json`, and the document head for a sample brand:
  `Serve_Given_A_Rebranded_Host_` (web-server-integration-tests).
- **Review round 1 fixes.** M1: the Design System page description reads "for the configured
  brand" instead of naming TimeWarp Enterprises; `BrandCopy_Should_` fails on "TimeWarp
  Enterprises" in any `.razor` file of a web project. M2: `tw-blazor-css-strategy` rule 8 says the
  seed comes from `BrandOptions.NormalizedSeedColor` and points at `tw-brand`.
- **Skill.** `skills/tw-brand/SKILL.md` (rule, surface table, role table, steps, icon commands,
  verify, do-not); AGENTS.md "Where to look" row; `tw-blazor-css-strategy` now says role tokens
  only; template-smoke `AssertSkillsShipped` requires `tw-brand`.
- **Runtime brand.** Assistant name stays the only runtime brand setting. Product display name
  was not made runtime: it feeds the prerendered `<title>`, manifest, and link preview at
  request time from options, and a runtime store would need its own cache and invalidation.
- **Evidence** (`evidence/`): `header-{light,dark}.png`, `home-{light,dark}.png`,
  `design-system-brand-{light,dark}.png`, `design-system-color-tokens-{light,dark}.png`,
  `tab-icon.png`, `served-documents.txt` (the served `/manifest.json` and `/brand.css`). Captured
  from the in-proc web host under InteractiveWebAssembly with a sample Northwind Traders brand
  (green primary, orange secondary, gold highlight); the WebAssembly runtime loaded its brand
  from `/brand.json`. The capture harness was a throwaway Playwright class, not committed.

### How to validate

- Smoke: `./bin/dev test --project web-spa-integration-tests --filter-class Brand` (also runs
  `BrandCopy_Should_`), then
  `./bin/dev test --project web-server-integration-tests --filter-class Serve_Given_A_Rebranded_Host_`,
  then `./bin/dev template-smoke`.
- Expect: 12/12 and 4/4 pass; template-smoke prints `Rebrand smoke: 26 files scanned, no template
  brand after a configuration-only rebrand.` and ends `Template smoke SUCCEEDED`. Open
  `evidence/home-light.png`: green frame, Northwind Traders logo and title, no TimeWarp mark.

### Gates

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `./bin/dev lint-skills` — 10 skills linted, 10 passed (includes `tw-brand`).
- `tests/container-apps/web/web-spa-integration-tests/configuration/brand-options-tests.cs` (`--filter-class BrandOptions_Should_`) — 10/10 passed.
- `tests/container-apps/web/web-spa-integration-tests/configuration/brand-role-token-tests.cs` (`BrandRoleTokens_Should_`) — 1/1 passed.
- Fix pass round 1 (M1, M2): `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- Fix pass: `tests/container-apps/web/web-spa-integration-tests/configuration/brand-copy-tests.cs` (`--filter-class BrandCopy_Should_`) — 1/1 passed.
- Fix pass: `tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs` (`DesignSystemPage_Should_`) — 6/6 passed.
- Fix pass: `./bin/dev lint-skills` — passed (`tw-blazor-css-strategy` edited).
- Fix pass: `ganda repo audit` — "Repository passes all audit checks."
- `tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs` (`TimeWarpPage_Should_`) — 17/17 passed.
- `tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs` (`DesignSystemPage_Should_`) — 6/6 passed.
- `tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs` (`Destinations_Should_`) — 8/8 passed.
- `tests/container-apps/web/web-server-integration-tests/brand-documents-tests.cs` + `link-preview-meta-tests.cs` (`Serve_Given_`) — 10/10 passed.
- `tests/container-apps/web/web-server-integration-tests/link-preview-tests.cs` (`Resolve_Should`) — 10/10 passed.
- `tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs` (`Document_Given_`) — 45/45 passed.
- `tests/container-apps/web/web-spa-playwright-tests/first-paint-playwright-tests.cs` (`FirstPaint_Given_PrerenderedShell`) — 3/3 passed.
- `dotnet run --file source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs` — 6/6 passed.
- These suite files are not standalone runfiles (no `JARIBU_MULTI` guard); they ran as targeted `./bin/dev test --project … --filter-class …` runs, and CI runs the full suites.
- `./bin/dev template-smoke` — `Template smoke SUCCEEDED` (default variant). Contoso.Shop rebrand: 26 files scanned, no template brand; SmokeDefault built, co-located runfiles and Jaribu aggregators passed. The first run (before the `wwwroot/js` exclude) failed on 8 hits in gitignored `wwwroot/js/*.js` build output that the template pack shipped; fixed by the template exclude.
- `--matrix` was not run: the template.json change (`appNameKebab` onlyIf, `wwwroot/js` exclude) is flag-independent; CI runs the matrix nightly.
- `ganda repo audit` — passed with 1 advisory warning (`vscode-window-icon`: `.vscode/settings.json` missing in this worktree); `ganda repo audit --fix` wrote the ignored local `.vscode/settings.json`; rerun: "Repository passes all audit checks."

## Review

- Round 1: Blocked — open M1, M2
- Fix pass (2026-11-25): M1 fixed, M2 fixed.
