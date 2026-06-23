# Adopt global design tokens (tokens.css)

Parent: 059. Depends on 059-002 (prefix choice).

## Goal

Create `web-spa/wwwroot/css/tokens.css` as the **single source of truth** for color, type
scale, radius, elevation, and status palette — plain CSS custom properties, no Tailwind.
Mirror the crunchit `tokens.css` shape
(`crunchit/.../source/web-spa/wwwroot/css/tokens.css`).

## Why

Tailwind's `tailwind.config.js` currently defines the palette (`primary/secondary/accent/
danger/warning/positive/info`) and font. When Tailwind is removed (059-006), that palette
must live somewhere. Tokens-as-CSS-variables is the agreed home and is strategy-agnostic
(works with isolation, scope-handle, and `::part()` alike).

## Tasks

- [ ] Create `wwwroot/css/tokens.css` with `:root { --twa-*: … }` covering:
      - color (map the existing Tailwind `primary/secondary/accent/danger/warning/positive/
        info` intents to concrete values; do NOT carry over Tailwind's full numeric scale
        unless actually used),
      - type scale (sizes currently expressed via `text-*` utilities + the Inter font),
      - radius, elevation/shadow, and status colors used by badges/alerts.
- [ ] Map any FluentUI tokens currently referenced in CSS (e.g.
      `var(--neutral-foreground-rest)` used by `.placeholder` in `styles/input.css`) —
      decide whether to keep consuming Fluent design tokens or define our own.
- [ ] Reference `tokens.css` from `web-server/components/App.razor` `<head>` (replacing the
      `css/site.css` Tailwind link) alongside the Blazor scoped-css bundle.
- [ ] Decide on the Inter font (`https://rsms.me/inter/inter.css` link vs self-host); record
      `--twa-font-sans`.

## Notes

crunchit's `tokens.css` is ~40 custom properties for a full portal — ours will be similar.
Keep it lean; only add tokens actually consumed.
