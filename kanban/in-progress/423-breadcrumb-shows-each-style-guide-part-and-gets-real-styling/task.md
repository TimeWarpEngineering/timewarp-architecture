# Breadcrumb shows each style-guide part and gets real styling

## Description

The page breadcrumb has two problems. Steve found both on the style guide after task 421.

1. **Every style-guide part adds another "Style Guide" crumb.** Visiting `/StyleGuide/brand` and
   then `/StyleGuide/color-tokens` shows `… / Style Guide / Style Guide / Style Guide`.
   `TwBreadcrumb` (TimeWarp.State.Plus) renders the `RouteState` history stack. Each part is a
   distinct URL, so `PushRouteInfo` pushes a new entry, and each entry takes the document title.
   That title is "Style Guide" for every part (`StyleGuidePage.Title`), so the crumbs can't be
   told apart.
2. **The breadcrumb has no styling.** `TimeWarpPage.razor.css` styles Bootstrap class names
   (`.breadcrumb`, `.breadcrumb-item`, `.active`, `.text-muted`). Since TimeWarp.State
   12.0.0-beta.10, `TwBreadcrumb` renders its own classes instead (`tw-breadcrumb`,
   `tw-breadcrumb__item`, `tw-breadcrumb__item--current`, `tw-breadcrumb__ellipsis`) and ships
   isolated CSS, with an optional `--tw-breadcrumb-divider` property. The shell rules match
   nothing. The pin here is 12.0.0-beta.15.

## Requirements

- **Part crumbs.** A style-guide part's crumb names the part (for example "Brand" or
  "Color Tokens"), not "Style Guide". The "All" view keeps "Style Guide".
- **Switching parts.** Moving from one style-guide part to another replaces the current crumb
  instead of adding one. Brand, then Color Tokens, reads `Style Guide / Color Tokens`.
  Steve chose replace over a crumb per part (2026-10-27).
- **Styling.** Restyle the trail against the classes `TwBreadcrumb` renders now, with brand
  tokens: link color, hover, current item, separator, ellipsis, spacing under the title. Delete
  the dead Bootstrap-class rules and update the `TimeWarpPage` Design region note that says
  `TwBreadcrumb` emits Bootstrap class names.
- If the fix belongs in TimeWarp.State.Plus (for example a way for a page to replace its entry),
  do it there and release it. Do not work around the library here
  (`release-only-when-platform-packages-change`: we own it).
- Browser tests: part crumb text, part-to-part switching, and that the computed style shows the
  new rules applied (for example the separator and link color).

## Checklist

- [x] Steve chooses replace vs a crumb per part (replace)
- [ ] Part crumbs show the part name
- [ ] Part-to-part navigation follows the chosen rule
- [ ] Breadcrumb styled against `tw-breadcrumb*` classes; dead Bootstrap rules removed
- [ ] `TimeWarpPage` Design region updated
- [ ] Playwright coverage for crumbs and styling
- [ ] `dev build` 0/0; spa integration and Playwright suites pass

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)

## Notes

- Code: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` (Design region
  and `TwBreadcrumb` placement), `TimeWarpPage.razor.css` (the `.twe-page__crumbs` rules),
  `features/style-guide/pages/StyleGuidePage.razor`.
- Library: timewarp-state `source/timewarp-state-plus/features/routing/` (`TwBreadcrumb.razor`,
  `tw-breadcrumb.md`, `route-state/route-state.push-route-info.cs`). `PushRouteInfo` updates the
  top entry in place for the same URL and truncates on revisit. A different URL always pushes.
- Part of Steve's style-guide feedback after task 421. Other items (the header demo buttons, the
  page-action placement rule, the xAI key policy location) go into a separate task.
