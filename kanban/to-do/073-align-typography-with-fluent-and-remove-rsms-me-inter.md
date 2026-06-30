# Align typography with Fluent and remove rsms.me Inter

Follow-up to 059-001 (design tokens). Closes the open Inter/rsms.me decision.

## Goal

Use **Fluent-aligned system typography** for custom HTML and plain CSS so it matches FluentUI v5
components. Remove the external `https://rsms.me/inter/inter.css` dependency.

## Why

Inter was carried over from the Tailwind era. FluentUI v5 already ships its own type stack (Segoe UI
Variable on Windows, system fallbacks elsewhere). Loading Inter from rsms.me means:

- A third-party CDN request on every page load
- Two font personalities on the same screen (Fluent primitives vs. `--twe-font-sans` content)
- An unresolved item left open in 059-001

Aligning with Fluent gives zero extra font downloads, consistent UI chrome, and no external dependency.

## Checklist

- [ ] Remove the `<link rel="stylesheet" href="https://rsms.me/inter/inter.css">` from
      `web-server/components/App.razor` (and any related comment).
- [ ] Update `--twe-font-sans` in `web-spa/wwwroot/css/tokens.css` to reference Fluent's own token:
      `--twe-font-sans: var(--fontFamilyBase, sans-serif);`. **Confirmed available** — FluentUI v5
      ships `--fontFamilyBase`/`--fontFamilyMonospace`/`--fontFamilyNumeric` and its own `reboot.css`
      uses `font-family: var(--fontFamilyBase)`; the value (Segoe UI Variable stack) is set at runtime
      by Fluent's design-token JS (`Microsoft.FluentUI.AspNetCore.Components.lib.module.js`). This
      auto-tracks whatever Fluent chooses, so custom HTML matches Fluent components exactly.
      - Caveat: the var is JS-set, so it's empty until Fluent's JS runs — keep a final `sans-serif`
        fallback (`var(--fontFamilyBase, sans-serif)`) to cover the prerender/pre-hydration window.
      - Fallback if you ever want zero JS dependency: an explicit
        `"Segoe UI Variable", "Segoe UI", system-ui, -apple-system, sans-serif` stack.
- [ ] Confirm `app.css` and any component CSS that sets `font-family` still consume
      `var(--twe-font-sans)` — no stray `Inter` references.
- [ ] Visual smoke-check: shell (`TimeWarpPage`), cards, tables, and Fluent buttons/inputs should
      read as one type system (no obvious mismatch between custom HTML and Fluent components).
- [ ] Note the decision in this task's Notes (which token/stack was chosen and why).

## Notes

- rsms.me is Rasmus Andersson's site — the official host for the Inter web font. We are **not**
  replacing Inter with another custom web font; we are dropping custom web fonts entirely.
- Do **not** self-host Inter as part of this task; the goal is Fluent alignment, not font substitution.
- Type **scale** tokens (`--twe-text-*`) stay as-is; only the sans stack changes unless visual review
  shows a sizing mismatch worth a follow-up.