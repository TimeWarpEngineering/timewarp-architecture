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

## Goal (implementer restatement)

The shell nav needs a bottom toggle that switches the existing icon rail and the full 240px column. At 1280px and wider the column starts full and the toggle can collapse it. From 880px through 1279px it still starts as the rail and the toggle can widen it. Below 880px the drawer stays as it is and the toggle is not shown. The click is a TimeWarp.State action, and the choice is stored in this browser so a reload keeps it. The stored choice has to be on the document before the nav paints, so a wide page does not flash the full column and then snap to the rail. The collapsed look reuses the rail rules already used for the mid-width breakpoint. The shell Design note and the layout skill describe that behavior.

## Checklist

- [x] Toggle at the bottom of the nav column, with accessible name and `aria-expanded`
- [x] Collapsed = existing rail (icons, tooltips, rail user card); rail CSS not duplicated
- [x] Width rules above, including the toggle hidden under the drawer breakpoint
- [x] TimeWarp.State action; choice persisted per browser; no first-paint flash
- [x] Design region and `tw-blazor-layout` updated
- [x] Playwright: collapse and expand at 1440px, the choice survives a reload, expand at 1100px,
      toggle hidden at 600px
- [x] `dev build` 0/0; spa integration and Playwright suites pass; `dev lint-skills` when
      `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)
- `ganda repo audit --fix` scaffolded an empty `glossary.md`. The check was failing because the file was missing. `kanban-landed` still warns that claims 070-001, 104-024, 104-025, 104-026, 123, 141, 198, and 244 have no usable worktree. Those claims were left as they are. Audit exit code after the fix is 0.

## Results

The nav column has a width toggle under the user card. Two icon buttons share one click, `ApplicationState.ToggleMenu`. The visible button is the one that matches the current column, named "Collapse navigation" or "Expand navigation", with `aria-expanded`. The choice is `twe.shellNav` in localStorage (`full` or `rail`). A blocking script in `App.razor` head sets `data-twe-nav` before the body is parsed. Tokens turn that, and the 880–1279 default, into `--twe-nav-rail`. The rail rules in the shell, the menu, and the user card live once under `@container twe-shell-nav style(--twe-nav-rail: 1)`. Below 880px the toggle is hidden and the drawer is unchanged.

### How to validate

Smoke: At 1440px open `/`. Click Collapse navigation. Reload. Click Expand navigation. Reload. At 1100px open `/` and click Expand navigation. At 600px open `/` and look for the toggle. With `twe.shellNav=rail` already stored, load `/` at 1440px with `blazor.web.js` blocked.

Expect: At 1440px the nav starts at 240px, the collapse button is shown with `aria-expanded=true`, the click narrows it to 68px, labels clip, the GitHub item's title is "GitHub", storage is `rail`, and a reload stays at 68px with Expand navigation. Expand returns 240px and `full`, and a reload stays there. At 1100px the nav starts at 68px and the toggle widens it to 240px with storage `full`. At 600px `[data-qa=NavWidthToggle]` is hidden. The blocked-script load at 1440px paints the 68px column and `data-twe-nav=rail` before Blazor runs. `./bin/dev build` is 0 warnings and 0 errors. `web-spa-integration-tests` passed 531. `web-spa-playwright-tests` passed. `./bin/dev lint-skills` passed.

## Notes

- Shell: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` (the nav
  column and user card) and `TimeWarpPage.razor.css` (`.twe-nav-column`, the 880–1279px rail
  media query). Nav: `components/NavMenu.razor` (rail watch and tooltips). Tokens:
  `wwwroot/css/tokens.css`.
- Part of Steve's style-guide feedback after task 421 (with tasks 423 and 424). It was split out
  because it changes the whole shell.
