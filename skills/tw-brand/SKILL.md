---
name: tw-brand
description: Rebrand a generated TimeWarp.Architecture app. The visual brand (product name, logo, icons, manifest colors, Fluent seed, role colors and their dark variants) is one BrandOptions section in web-server configuration plus image files; the assistant name is the only runtime (admin) brand setting; no shell code is edited. Use when changing an app's name, logo, colors, favicon, PWA icons, or link-preview image, when adding component CSS that needs a brand color, or when /brand.css, /manifest.json, or /brand.json look wrong.
---

# Brand (configuration only)

**Rule.** The app's agent sets the visual brand once, in configuration: the `BrandOptions`
section of `source/container-apps/web/projects/web-server/appsettings.json`, plus the image files
it names. Identity words an admin changes at runtime (the assistant name on the AI page) are
not configuration. Nothing in a rebrand edits shell code (`.razor`, `.razor.css`, `tokens.css`,
`App.razor`).

## Where the brand comes from

| Surface | Source |
|---------|--------|
| `<title>`, home heading, Brand card | `BrandOptions.ProductName` |
| App-bar and focused-page logo | `LogoSrc`, `LogoAlt` (empty = `ProductName`), `LogoWidth`, `LogoHeight` |
| Fluent component ramp | `SeedColor`, `ExactRamp` (written to `body[data-theme-color]` and Fluent's theme storage) |
| Shell colors (`/brand.css`) | `Tokens`: role colors and their dark variants |
| Tab icon, iOS icon | `FaviconSrc`, `AppleTouchIconSrc` (the `<link>` tags in `App.razor`) |
| PWA manifest (`/manifest.json`, served) | `ProductName`, `ThemeColor` (empty = seed), `BackgroundColor` (empty = `#fafafb`), `Icon192Src`, `Icon512Src` |
| Open Graph / Twitter image | `OgImageSrc`; `SiteMetadata.SiteName`, `Title`, `ImageAlt` default to the name and logo alt |
| SPA after hydration | `GET /brand.json` (the same section; the SPA has no copy) |
| Assistant name | Runtime admin setting (AI page, `UpdateAssistantName`). Not configuration. |

Paths are relative (`images/brand/logo.svg`), root-relative (`/images/logo.svg`), or absolute
`https://` URLs. The validator rejects anything else at startup, and every color must be
`#RGB` or `#RRGGBB`.

## Role tokens

Component CSS reads role tokens only. `/brand.css` overrides exactly these, so anything else
keeps the template color after a rebrand.

| `BrandOptions:Tokens` key | CSS custom property | Use |
|---------------------------|---------------------|-----|
| `Primary`, `PrimaryHover`, `PrimaryActive` | `--twe-primary`, `-hover`, `-active` | Header frame, primary fills, selected states |
| `PrimarySoft` | `--twe-primary-soft` | Light primary tint, gradients |
| `OnPrimary` | `--twe-on-primary` | Text on a primary fill |
| `Secondary`, `SecondaryHover`, `SecondaryActive` | `--twe-secondary`, `-hover`, `-active` | Footer frame, links, focus outlines |
| `SecondaryBright` | `--twe-secondary-bright` | Bright secondary step, gradients |
| `Highlight`, `HighlightSoft` | `--twe-highlight`, `--twe-highlight-soft` | Accent rules, active status dot |
| `Info` | `--twe-info` | Info status color |
| `PrimaryDark` | `--twe-primary-text` in dark mode | Primary text, accent text, focus ring on dark paper |
| `SecondaryDark` | `--twe-secondary-text` in dark mode | Links and info text on dark paper |
| `HighlightDark` | `--twe-highlight-text` in dark mode | Shell accent rules on dark paper |

`tokens.css` keeps a hue palette (`--twe-purple*`, `--twe-blue*`, `--twe-green*`). It only seeds
the role defaults. A `var(--twe-purple…)`, `var(--twe-blue…)`, or `var(--twe-green…)` under
`features/` or `components/` fails `BrandRoleTokens_Should_`. Text on paper uses the derived
roles (`--twe-link`, `--twe-primary-text`, `--twe-accent-text`, `--twe-focus-ring`); see
`tw-blazor-css-strategy`.

## Rebrand steps

1. **Pick the colors.** Seed = the brand's main color; it is usually `Primary` too.
   - `PrimaryHover`: about 10% lighter than `Primary`. `PrimaryActive`: about 25% darker.
     `PrimarySoft`: a pale tint. Same pattern for secondary; `SecondaryBright` is a vivid step.
   - `OnPrimary` must reach 4.5:1 on `Primary`.
   - Dark variants: start from the soft or bright step and lighten until it reaches **4.5:1 on
     `#303030`** for `PrimaryDark` and `SecondaryDark` (text), **3:1** for `HighlightDark`
     (an indicator). Check each pair with a contrast checker before committing.
2. **Make the images from one square SVG mark** (`mark.svg`) and one horizontal lockup
   (`logo.svg`). With `rsvg-convert` (librsvg) and ImageMagick:

   ```bash
   cd source/container-apps/web/projects/web-spa/wwwroot
   mkdir -p images/brand
   cp ~/brand/logo.svg images/brand/logo.svg
   cp ~/brand/mark.svg images/brand/favicon.svg            # modern browsers take an SVG icon
   rsvg-convert -w 32  -h 32  ~/brand/mark.svg -o favicon-32x32.png
   rsvg-convert -w 180 -h 180 -b white ~/brand/mark.svg -o apple-touch-icon.png
   rsvg-convert -w 192 -h 192 ~/brand/mark.svg -o icon-192.png
   rsvg-convert -w 512 -h 512 ~/brand/mark.svg -o icon-512.png
   rsvg-convert -h 200 ~/brand/logo.svg -o /tmp/logo.png
   magick -size 1200x630 xc:'#ffffff' /tmp/logo.png -gravity center -composite images/og-image.png
   ```

   Overwriting the default icon paths needs no key change. New paths need the matching key.
3. **Set the section** in `web-server/appsettings.json` (keys you omit keep the defaults):

   ```json
   "BrandOptions": {
     "ProductName": "Northwind Traders",
     "LogoSrc": "images/brand/logo.svg",
     "LogoWidth": 180,
     "LogoHeight": 40,
     "SeedColor": "#0b6e4f",
     "ExactRamp": false,
     "FaviconSrc": "images/brand/favicon.svg",
     "Tokens": {
       "Primary": "#0b6e4f", "PrimaryHover": "#11865f", "PrimaryActive": "#06492f", "PrimarySoft": "#a7e3c8",
       "OnPrimary": "#ffffff",
       "Secondary": "#c2571a", "SecondaryHover": "#d86a2b", "SecondaryActive": "#8a3a0d", "SecondaryBright": "#ff9a52",
       "Highlight": "#d4a017", "HighlightSoft": "#f7e3a1", "Info": "#1f6fb2",
       "PrimaryDark": "#7fd8b0", "SecondaryDark": "#ffb27d", "HighlightDark": "#f2c94c"
     }
   }
   ```

   Per-environment values (a staging color) go in `appsettings.<Environment>.json` or
   environment variables (`BrandOptions__SeedColor`). The SPA reads whatever the server bound.
4. **Delete the template logo** `wwwroot/images/timewarp-horizontal.svg` once `LogoSrc` points
   elsewhere.
5. **Copy that is not brand configuration.** Review and rewrite as product copy:
   `SiteMetadata:Description` (link previews), `WebAuthnOptions:RpName` (passkey prompts), and
   the agent documents in `wwwroot` (`index.md`, `llms.txt`, `auth.md`, `.well-known/agent.json`,
   `.well-known/agent-card.json`, `.well-known/mcp.json`). If `Icon192Src` moves, update their
   `iconUrl`.

## Verify

- `dev build`, then run the app (`dev run`) and check:
  - `/brand.css` shows the `:root` roles and a `body[data-theme="dark"]` block with the three
    dark variants; `/manifest.json` shows the name, colors, and icons; `/brand.json` shows the
    section.
  - The header, the tab icon, and `/DesignSystem` (Brand and Color Tokens cards), in light and
    dark mode (theme button in the app bar).
- No template mark is left in the brand surfaces:

  ```bash
  rg -n -i 'timewarp|#55409c' source/container-apps/web/projects/web-spa/wwwroot \
    source/container-apps/web/projects/web-server/components/App.razor
  ```

  Hits that name a TimeWarp library (`TimeWarp.State`, `TimeWarp.Identity`, `timewarp-identity`,
  `x-timewarp-*`), a code identifier (`TimeWarpPage`), or a `tokens.css` hue palette line are
  dependencies, not brand. `dev template-smoke` runs this check on a generated app.

## Do not

- Do not edit `tokens.css`, `.razor.css`, or `App.razor` to change a brand color, icon, or name.
- Do not read a hue palette token in component CSS. Add a role token instead (a `BrandTokenOptions`
  property, its `--twe-*` declaration in `tokens.css`, and its line in `BrandStylesheet.Render`).
- Do not add a `BrandOptions` section to `web-spa/wwwroot/appsettings.json`. The server is the
  one source; the SPA loads `/brand.json`.
- Do not make a brand value a `dotnet new` parameter. `sourceName` renames the product; the rest
  is configuration.

## Code

`web-spa/configuration/`: `BrandOptions`, `BrandTokenOptions`, `BrandOptionsValidator`,
`BrandStylesheet` (`/brand.css`), `BrandDocument` (`/brand.json` and the SPA loader),
`WebManifest` (`/manifest.json`), `BrandThemeScript`. `web-server/brand-endpoints.cs` maps the
three documents. Tests: `BrandOptions_Should_`, `BrandRoleTokens_Should_`
(web-spa-integration-tests), `Serve_Given_A_Rebranded_Host_` (web-server-integration-tests).
