# Breadcrumb shows each style-guide part and gets real styling

## Description

Replace the history breadcrumb with a **location** breadcrumb (where this page sits in the app,
as in the Azure portal), built from the page registry, and style it. Steve decided 2026-10-28:
location-based only, no history trail.

Today `TimeWarpPage` renders `TwBreadcrumb` (TimeWarp.State.Plus), which shows the `RouteState`
history stack: every visited URL, labelled with the document title. That has two problems:

1. **It shows history, not location.** Every style-guide part has the title "Style Guide", so
   clicking parts stacks `Style Guide / Style Guide / Style Guide`. Arriving from another page,
   by Ctrl-K, a bookmark or a reload, gives a different trail for the same page. The browser's
   Back button already covers history.
2. **It is unstyled.** `TimeWarpPage.razor.css` styles Bootstrap class names (`.breadcrumb`,
   `.breadcrumb-item`). `TwBreadcrumb` stopped rendering those in TimeWarp.State 12.0.0-beta.10,
   so the rules match nothing.

## Requirements

- **Trail from the page registry.** The trail is Home, then each ancestor page, then the current
  page. A page's parent is the registered page whose route is the longest literal prefix of its
  route:
  - `/StyleGuide/{Part}` → Style Guide;
  - `/Admin/Roles/{id}` → Roles;
  - and so on for every page.
  
  Ancestors are links. The current page is text with `aria-current="page"`. Nav groups
  ("Developer", "Admin") are not crumbs, because they are not pages. If a group heading has its
  own page, it is a crumb like any other page.
- **Generate the hierarchy.** Derive the parent links from the routes the page source generator
  already emits (`PageRegistryEntry` in
  `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`), either
  at generation time or from the registry at run time. Do not hand-keep a parent list
  (`prefer-analyzers-sourcegen-over-inference`).
- **Labels.** A crumb's label is the page's nav title. A page with route parameters supplies the
  label for its own crumb. Examples:
  - The style-guide part crumb is the part title ("Color Tokens"), and `/StyleGuide` (All) is
    "Style Guide".
  - The role editor crumb is the role name once loaded.
  
  Add the smallest `TimeWarpPage` parameter that allows this, or reuse `Title` where it already
  matches.
- **Every route to a page gives the same trail.** Examples:
  - `/StyleGuide/color-tokens` reads `Home / Style Guide / Color Tokens`, whether you got there
    by clicking parts, by Ctrl-K, by a direct URL or after a reload.
  - `/Admin/Roles/{id}` reads `Home / Roles / <role name>`.
- **Remove the history trail from the shell.** Remove `TwBreadcrumb`, `PushRouteInfo` and
  `NoSubRouteState.PushRouteInfo()` from `TimeWarpPage`, plus the `ShowInBreadcrumbs`
  push logic. Keep a way for 403, 404 and auth pages to show no trail. Keep `TwPageTitle` for
  `document.title`. Leave other `RouteState` uses (for example `TodoItemFormContainer`'s
  `ChangeRoute`) alone.
- **Render and style it in this repo.** Render a native `<nav aria-label="Breadcrumb"><ol>`
  trail, styled with brand tokens in `TimeWarpPage.razor.css`: link color, hover, current item,
  separator, and space under the title. Delete the dead Bootstrap-class rules. Collapse long
  trails with an ellipsis only if one actually exceeds the width.
- **Docs.** Update the `TimeWarpPage` Design region (the trail lines and the `TwBreadcrumb`
  note), the `RoleDetailPage`/`RolesListPage` comments that cite `TwBreadcrumb`, and
  `tw-blazor-layout` where it describes the trail. The no-per-page-Back-button rule stays.
- **This repo only.** Make no change in any other repository. Do not create tasks, branches or
  PRs elsewhere. If a library change seems necessary, stop and report it in `## Results`;
  Steve decides.
- **Tests.**
  - Unit tests for parent resolution: the longest literal prefix, a parameterized child, and a
    page with no parent.
  - Playwright:
    - the part crumb reads `Home / Style Guide / Color Tokens`;
    - part-to-part clicks keep one part crumb;
    - opening a part from Ctrl-K on another page gives the same trail;
    - the role editor trail;
    - a computed style check that the new rules apply.

## Checklist

- [x] Steve chose a location breadcrumb only (2026-10-28)
- [ ] Parent resolution generated from page routes, with unit tests
- [ ] Parameterized pages supply their crumb label (style-guide part, role editor)
- [ ] History trail (`TwBreadcrumb` / `PushRouteInfo`) removed from `TimeWarpPage`; error pages show no trail
- [ ] Native breadcrumb styled with tokens; dead Bootstrap rules removed
- [ ] Design region, page comments and `tw-blazor-layout` updated
- [ ] Playwright coverage as listed
- [ ] `dev build` 0/0; `dev regenerate-generated` golden diff empty; spa integration and
      Playwright suites pass; `dev lint-skills` when `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)
- First run (2026-10-27) stopped. It built on a history-trail fix in another repo (timewarp-state
  PR #660, task 131). Both were dropped when Steve chose a location breadcrumb.

## Notes

- Shell: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` and
  `TimeWarpPage.razor.css` (`.twe-page__crumbs`). Style guide:
  `features/style-guide/pages/StyleGuidePage.razor` and `StyleGuideParts`. Roles:
  `features/admin/roles/pages/`.
- Page registry consumers to keep working: `navigation-destinations.cs`,
  `command-palette-roster.cs`, `agent-navigate.cs`, `page-agent-context.cs`.
- Part of Steve's style-guide feedback after task 421 (with tasks 424 and 426).
