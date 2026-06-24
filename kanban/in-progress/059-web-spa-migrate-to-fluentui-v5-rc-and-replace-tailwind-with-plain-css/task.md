# web-spa: migrate to FluentUI v5 RC and replace Tailwind with plain CSS

## Description

Epic. Migrate `source/container-apps/web/web-spa` (and `web-server`) off:

- **FluentUI Blazor v4** (`4.14.2`) → **v5 RC** (`5.0.0-rc.x`), and
- **Tailwind CSS / npm / TypeScript toolchain** → **hand-written plain CSS** built on
  global design tokens.

Goal stated by Steven: the **cleanest, best** solution. **No backwards compatibility,
no tech debt.** This is a forward port, not a compatibility shim.

## Reference implementation (the proven pattern to copy)

`~/worktrees/github.com/Crunchitfs/crunchit/Cramer-2026-05-29-initial/source/web-spa`
(branch `Cramer/2026-05-29/initial`) already runs FluentUI **v5.0.0-rc.3-26138.1** with
**zero Tailwind/npm** — just `wwwroot/css/tokens.css` + scoped `.razor.css` + a documented
CSS tiering. Mirror its structure. Its CSS strategy decision lives in that repo's
`kanban/to-do/022-research-css-strategy-for-components-isolated-css-pain-points.md` — read
it before starting 059-002.

Key v5 facts already verified there:
- v5 still uses **shadow-DOM web components** for interactive primitives
  (`fluent-button`, `fluent-text` have open shadow roots) → style only via `::part()` +
  CSS custom properties. v5 did **not** remove the CSS-isolation pain.
- `FluentStack` etc. are **light-DOM** divs → stylable by class, but NOT from a parent's
  isolated `.razor.css` (scope attr isn't stamped on a child component's root).
- v4 is supported until **Nov 2026**, so there is no hard deadline; we move now to stay clean.

## Decisions captured (2026-06-22)

- **CSS strategy** documented as an exportable skill: `skills/blazor-css-strategy/SKILL.md`
  (root `skills/` = home for exportable skills). Prefix = `--twa-*`. (059-002 done.)
- **Splitter survives:** `FluentMultiSplitter`(+Pane) IS in v5 RC — `TimeWarpPage` keeps it.
- **Sidebar/nav-bar:** rebuild with FluentUI v5 `FluentLayout`/`FluentLayoutItem`/
  `FluentLayoutHamburger` + `FluentNav`/`FluentNavItem`, NOT hand-written CSS (shrinks 059-005).
- The repo already uses the Tier-2 scope-handle pattern (`TimeWarpPage` `.{Id}` + `<style>`).

## Child tasks (suggested order)

1. **059-002** — Decide/document CSS strategy (isolation-first hybrid). *Do this first; it
   governs everything else.*
2. **059-001** — Adopt global `tokens.css` design tokens.
3. **059-003** — Upgrade packages to v5 RC + wire `AddFluentUIComponents` / `FluentProviders`.
4. **059-004** — Per-component v5 API migration + shadow-DOM audit.
5. **059-005** — Rewrite the Tailwind-only UI (sidebar/nav-bar/forms/modals) as plain CSS.
6. **059-006** — Remove the Tailwind/npm/TypeScript toolchain + csproj build hacks.
7. **059-007** — Propagate to the `TimeWarp.Architecture/` template.

001–005 are interdependent and land together; 006 is the cleanup once nothing references
Tailwind; 007 is the template propagation.

## Current-state facts (web-spa, captured 2026-06-22)

- FluentUI `4.14.2`, 3 packages: `.Components`, `.Components.Emoji`, `.Components.Icons`.
- ~58 Fluent component instances, ~25 distinct types. Heaviest: `FluentLabel` (22),
  `FluentButton` (12), `FluentStack` (9), `FluentSpacer` (7), `FluentNavLink`/`NavGroup`/
  `NavMenu`, `FluentIcon`, `FluentMultiSplitter`(+Pane), `FluentMenu`/`MenuItem`,
  `FluentCard`, `FluentTextField`, `FluentDialog`, `FluentPersona`, `FluentProgressRing`,
  `FluentDivider`. No `FluentDataGrid`/`QuickGrid` in markup, no `FluentDesignTheme`.
- Providers wired in `web-spa/components/layouts/` (`FluentUIRequiredFeatures`,
  Toast/Dialog/Tooltip/Menu/MessageBar/KeyCode providers).
- Tailwind `3.4.3` → `wwwroot/css/site.css`, linked in `web-server/components/App.razor:16`;
  `fluent.css` linked at `:19`. Custom color theme + `typography`/`aspect-ratio` plugins,
  `preflight: true`.
- **45 razor files** use Tailwind utilities. The **sidebar + nav-bar features (~20 files)
  are 100% raw HTML + Tailwind, zero FluentUI** — these are the real work.
- Only **2** `.razor.css` files exist today.
- `TimeWarp.Components` (1.0.0-beta.2) is style-agnostic, net10.0, **no FluentUI dep** —
  decoupled from this migration.

## Definition of done

- [ ] web-spa builds and runs on FluentUI v5 RC with no v4 references.
- [ ] No Tailwind/npm/TypeScript artifacts remain (config, lockfiles, node_modules, targets).
- [ ] All styling is plain CSS via the documented strategy (tokens + isolation/scope-handle/`::part`).
- [ ] Visual parity verified against the running app (Playwright screenshots).
- [ ] Template (059-007) updated to match.
