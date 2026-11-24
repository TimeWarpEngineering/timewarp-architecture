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

## Checklist

- [ ] `skills/tw-brand/SKILL.md` + AGENTS.md row; `dev lint-skills` green
- [ ] Single `BrandOptions` source; duplicate test deleted
- [ ] Icon set, manifest colours, `LogoAlt`, dark role variants in `BrandOptions`; `/brand.css` dark block
- [ ] Component CSS on role tokens only; guard check in place; Color Tokens card lists roles
- [ ] Template-smoke rebrand assertion passes with zero TimeWarp hits
- [ ] Screenshots in `evidence/`
- [ ] `dev build` 0/0, web-spa integration tests, `dev template-smoke`, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's branding question ahead of the first generated app.
  Inventory in this session: no surface is a template parameter; assistant name is the only
  runtime setting; 379-014 set the model.
