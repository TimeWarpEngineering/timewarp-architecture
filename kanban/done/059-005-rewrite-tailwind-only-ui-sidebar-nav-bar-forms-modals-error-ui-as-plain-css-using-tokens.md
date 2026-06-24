# Rewrite Tailwind-only UI as plain CSS

Parent: 059. The largest piece of the epic. Depends on 059-001 (tokens) + 059-002 (strategy).

## Progress (2026-06-22) — main shell verified rendering on v5

The primary shell (`SitePage` = `TimeWarpPage`/FluentMultiSplitter + `LeftPane_Main` FluentNav
+ header/footer) renders correctly on v5 (verified via Playwright). Re-scoped from "rebuild 20
files": the bespoke Tailwind `DarkNavBar`/nav-bar is **dead code** (only in comments), and the
Tailwind `SidebarPage` is used only by ~5 dev/example pages.

Fixed so far:
- `fluent.css` (v4 bundle imports) → direct v5 `bundle.scp.css` link in App.razor; deleted
  fluent.css. CSS loads 200; design tokens via auto-injected `default-fuib.css`.
- Profile menu: was inline/always-open → wrapped items in **`FluentMenuList`** and moved the
  avatar trigger INSIDE `<FluentMenu>` via `slot="@FluentSlot.Trigger"` (the external
  `Trigger=<id>` attribute did not render in rc.3). Verified: opens on click, hidden by default.
- Removed `LeftPane_Footer` placeholder stub text.
- HomePage alert corrected ("Fluent UI Blazor v5 and plain CSS"; removed Tailwind link + dead
  `mt-4 w-fit`).

### Still TODO
- [ ] **Brand theming**: shell is default FluentUI blue/gray — apply navy `--twa-*` (needs a
      decision on setting the v5 FluentUI brand ramp vs. our own CSS).
- [ ] HomePage demo cleanup: `FluentLabel Typo=...` examples (deprecated → `FluentText`), and
      the `PagePane_FooterContent` demo slot placeholder text.
- [ ] `SidebarPage` dev pages (Counter/Test/UserClaims/AlertExample/Forbidden) carry dead
      Tailwind — restyle or delete.
- [ ] Delete dead `DarkNavBar`/nav-bar + sidebar feature dirs if confirmed unused.

## Goal

Convert all 45 razor files using Tailwind utility classes to plain CSS via the documented
strategy (tokens + isolation / scope-handle). **No Tailwind classes may remain** after this.

## DECISION (Steven, 2026-06-22): re-express sidebar + nav-bar with FluentUI v5

Do **NOT** hand-write CSS to reproduce the bespoke Tailwind sidebar/nav-bar. Rebuild them
with FluentUI v5 layout primitives (all confirmed present in `5.0.0-rc.3`):

- App shell / sidebar / header → **`FluentLayout`** + **`FluentLayoutItem`** (`Area=`) +
  **`FluentLayoutHamburger`** for the mobile toggle. (crunchit `MainLayout.razor` is the
  reference shape.)
- Navigation → **`FluentNav`** + **`FluentNavItem`** (+ `FluentNavCategory` /
  `FluentNavSectionHeader` for the grouped sections).

This collapses most of "group A" below from hand-rolled CSS into FluentUI composition; the
remaining CSS is only spacing/brand tweaks via the Exception-B scope-handle pattern (see
`skills/blazor-css-strategy`). Re-scope the file list once the layout is rebuilt — many of
the ~20 sidebar/nav files may be deleted rather than restyled.

## Scope split

### A. Hard rewrites — pure HTML+Tailwind, zero FluentUI (~20 files)

These are full responsive layouts that exist only as utility strings; they need genuine
hand-written CSS (flexbox/grid, breakpoints, hover/focus, the dark gray-900 sidebar theme):

- `features/sidebar/components/sidebar/` — `Sidebar`, `SidebarPage`, `SidebarLink`,
  `SidebarNavSection`, `SidebarMobileMenu`, `HeaderBar`, `HamburgerButton` (216 lines total)
- `features/sidebar/components/sections/` — `Top/Bottom/Pages/ExternalLinks/SwaggerDocs`
  `SidebarNavSection`
- `features/application/components/nav-bar/` + `nav-bar/components/` — `DarkNavBar`,
  `MobileMenu`, `MobileMenuButton`, `NavigationLinks`, `NavBarLink`, `Logo`, `AlertButton`
- `features/account/pages/login-page/` — `LoginPage`, `LoginForm`, `RegisterPasskey`
- `features/application/components/ModalContainer.razor`, `modals/assembly-info/*`
- error UI block in `web-server/components/App.razor:34` (`#blazor-error-ui` + its Tailwind
  classes) → move to `tokens.css`/global or a small `app.css`.

### B. Light replacements (~25 files)

Files using a handful of spacing/flex utilities (`m-*`, `p-*`, `flex`, `gap-*`). Convert to
scoped `.razor.css` (Tier-1) or a scope-handle `<style>` (Tier-2) — e.g. `SiteFooter`,
`StackedPage`, `FormContainer`, weather-forecast `TableHeader`/`Cell`/page, to-do forms,
counter/event-stream/superhero/developer pages, `ProfileButton`, `HomePage`, `ForbiddenPage`.

## Tasks

- [ ] Replace Tailwind `preflight` with an explicit minimal reset (or rely on FluentUI v5's
      baseline) — `body class="font-sans antialiased"` in `App.razor` must keep working.
- [ ] Re-implement the `@tailwindcss/typography` (`prose`) and `aspect-ratio` usages, if any
      remain after audit, as plain CSS.
- [ ] Rewrite group A as plain CSS (component-scoped where the root is native; scope-handle
      where it wraps Fluent). Match current visuals — verify with Playwright screenshots.
- [ ] Convert group B mechanically to scoped `.razor.css`.
- [ ] Grep proves zero Tailwind utility classes remain across `web-spa` + `web-server`.

## Notes

This is design work, not a mechanical class swap — budget accordingly. Consider whether the
custom sidebar/nav should be re-expressed using FluentUI v5 `FluentLayout`/`FluentNav`
(as crunchit does) instead of hand-rolled CSS, which would shrink this task significantly.
Flag that decision to Steven before starting group A.
