# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** branch vs master, product paths. `App.razor` head script, `ShellNavScript`, `ApplicationState` (ToggleMenu, LoadShellNav), `TimeWarpPage.razor` and `.razor.css`, `NavMenu.razor.css`, `UserCard.razor.css`, `tokens.css`, `shell-rail.ts`, `tw-blazor-layout` SKILL.md, the shell breakpoint integration test, and the new shell-nav Playwright test.

## Summary

The change adds one width gate, `--twe-nav-rail`. `tokens.css` sets it from the 880–1279 default and from `html[data-twe-nav]`. The rail rules for the shell, the menu, and the user card query it once with `@container twe-shell-nav style(--twe-nav-rail: 1)`, so the rail CSS has one copy. A blocking head script (the same pattern as `BrandThemeScript`) applies the stored `twe.shellNav` before the body is parsed, so there is no first-paint flash. The toggle dispatches `ApplicationState.ToggleMenu()`. The handler works out the current width from storage and the viewport, then stores the opposite. `shell-rail.ts` also watches `data-twe-nav`, so rail tooltips follow the toggle.

Every requirement and every checklist item is met. The implementer restatement matches the task, so there is no misread. Gates were re-run in this worktree: `./bin/dev build` gave 0 warnings and 0 errors, and `dev test -g playwright --no-build` passed 31 of 31, including `NavWidth_Given_Shell`. The compiled `Web.Spa.styles.css` keeps the `@container` blocks with scoped selectors. I found no bugs. I recorded two low-severity observations.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.toggle-menu.cs:18
- Description: Renaming `ToggleMenu` to `ToggleMenuActionSet` was needed to get the generated `ApplicationState.ToggleMenu()` method. It also puts the action on the derived action roster, which the Ctrl-K palette lists (`action-roster-source-generator.cs`, the `*ActionSet` suffix). The old class was not on the roster. `LoadShellNav` opts out with a reason, but the Design region does not say that ToggleMenu is now a palette command. Below 880px, a palette run stores a choice that has no visible effect until the window is wider.
- Suggestion: Choose one, and record the choice in the ToggleMenu Design region. Either keep it as a keyboard path to the toggle, or add `[CommandPaletteOptOut("…")]`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:136
- Description: This is the repo's first container style query on a custom property (`@container … style(--twe-nav-rail: 1)`). In a browser that does not support custom-property style queries, no rail rule applies at any width. The 880–1279 default rail and the collapse toggle then do nothing, and the column stays 240px. The old media query worked everywhere. The change degrades to a usable full column, not a broken one. Chromium proves it (the Playwright suite), and no Firefox browser is installed here to check.
- Suggestion: Add a line to the `tokens.css` comment or the TimeWarpPage Design region that names the browser support this needs and what happens without it (the full column).
- Status: open
