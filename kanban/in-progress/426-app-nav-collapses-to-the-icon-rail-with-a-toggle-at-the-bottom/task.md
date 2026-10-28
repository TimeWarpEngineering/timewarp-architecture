# App nav collapses to the icon rail with a toggle at the bottom

## Description

The app's left nav can't be collapsed. The shell already has an icon rail
(`--twe-shell-rail-width`, 68px), but only a media query picks it, at 880px through 1279px. At
1280px and wider the nav is always the full 240px (`--twe-shell-nav-width`). On wide pages with
their own side column, such as the style guide's catalog, that leaves the content little room.

Steve's reference is Cloudflare's dashboard: a toggle button pinned at the bottom of the nav
column collapses it to icons and expands it again.

## Requirements

- **Toggle.** Add a button at the bottom of the nav column, below the pinned user card. It
  collapses the nav to the existing icon rail and expands it back. It has an icon and an
  accessible name that says what it will do ("Collapse navigation" / "Expand navigation"), and
  `aria-expanded`.
- **Collapsed look.** It is the same as the 880–1279px rail: icons only, labels hidden, item
  tooltips on (`NavMenu`'s rail watch already sets tooltips when labels are clipped), and the
  user card in its rail form. Reuse the rail rules. Don't add a second copy of them.
- **Widths.**
  - At 1280px and wider, the toggle chooses full or rail.
  - At 880–1279px the rail is still the default, and the toggle can expand it to full width.
  - Below 880px FluentLayout's drawer is unchanged, and the toggle is hidden.
- **State.** Clicking the toggle dispatches a TimeWarp.State action. A page-local field is not
  enough (`all-user-interactions-are-state-actions`). The choice is remembered per browser
  across reloads as a per-viewer preference, not a server setting. Restoring it must not flash
  the full nav before collapsing on first paint. If that can't be avoided under prerender,
  record why in the Design region.
- **Docs.** Update the `TimeWarpPage` Design region and `tw-blazor-layout` where they describe
  the nav column and the rail.

## Checklist

- [ ] Toggle at the bottom of the nav column, with accessible name and `aria-expanded`
- [ ] Collapsed = existing rail (icons, tooltips, rail user card); rail CSS not duplicated
- [ ] Width rules above, including the toggle hidden under the drawer breakpoint
- [ ] TimeWarp.State action; choice persisted per browser; no first-paint flash
- [ ] Design region and `tw-blazor-layout` updated
- [ ] Playwright: collapse and expand at 1440px, the choice survives a reload, expand at 1100px,
      toggle hidden at 600px
- [ ] `dev build` 0/0; spa integration and Playwright suites pass; `dev lint-skills` when
      `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)

## Notes

- Shell: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` (the nav
  column and user card) and `TimeWarpPage.razor.css` (`.twe-nav-column`, the 880–1279px rail
  media query). Nav: `components/NavMenu.razor` (rail watch and tooltips). Tokens:
  `wwwroot/css/tokens.css`.
- Part of Steve's style-guide feedback after task 421 (with tasks 423 and 424). It was split out
  because it changes the whole shell.
