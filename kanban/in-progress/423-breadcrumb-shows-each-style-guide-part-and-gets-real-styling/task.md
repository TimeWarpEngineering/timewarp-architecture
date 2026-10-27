# Breadcrumb shows each style-guide part and gets real styling

## Goal (implementer restatement)

The shell shows where the current page sits, the way the Azure portal does, and that trail is the same no matter how you arrived. Home comes first, then each ancestor page, then the current page. An ancestor is the registered page whose route is the longest literal prefix of this page's route (`/StyleGuide/{Part}` under Style Guide, `/Admin/Roles/{id}` under Roles). Nav group headings are not crumbs. Ancestor crumbs are links that use the page's nav title. The current crumb is text with `aria-current="page"`, and a page with route parameters can name that crumb (the style-guide part title, the role name once it has loaded).

`TimeWarpPage` stops rendering the `RouteState` history stack (`TwBreadcrumb`, `PushRouteInfo`). 403, 404, and auth pages can still omit the trail. `TwPageTitle` still sets `document.title`. The trail is a native breadcrumb nav, styled with brand tokens, and a long trail ellipsizes only when it does not fit. Parent links are derived from the routes the page source generator already emits, not a hand-kept list. Docs that describe the old trail are updated. The no-per-page-Back-button rule stays. This repo only.

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
- [x] Parent resolution generated from page routes, with unit tests
- [x] Parameterized pages supply their crumb label (style-guide part, role editor)
- [x] History trail (`TwBreadcrumb` / `PushRouteInfo`) removed from `TimeWarpPage`; error pages show no trail
- [x] Native breadcrumb styled with tokens; dead Bootstrap rules removed
- [x] Design region, page comments and `tw-blazor-layout` updated
- [x] Playwright coverage as listed
- [x] `dev build` 0/0; `dev regenerate-generated` golden diff empty; spa integration and
      Playwright suites pass; `dev lint-skills` when `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)
- First run (2026-10-27) stopped. It built on a history-trail fix in another repo (timewarp-state
  PR #660, task 131). Both were dropped when Steve chose a location breadcrumb.
- Implementation: 01a17e6b-0a10-7743-bc27-f491b739132b (2026-10-28). Parents come from
  `PageRegistry.All` at run time, so the page generator and the ingress golden stayed put.
  A Debug restore had left `TimeWarp.State.DevTools.Wasm` in the shared NuGet graph; a
  Release restore dropped it, and the Release boot manifest no longer requests that assembly.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-27T18:21:20Z

## Notes

- Shell: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor` and
  `TimeWarpPage.razor.css` (`.twe-page__crumbs`). Style guide:
  `features/style-guide/pages/StyleGuidePage.razor` and `StyleGuideParts`. Roles:
  `features/admin/roles/pages/`.
- Page registry consumers to keep working: `navigation-destinations.cs`,
  `command-palette-roster.cs`, `agent-navigate.cs`, `page-agent-context.cs`.
- Part of Steve's style-guide feedback after task 421 (with tasks 424 and 426).

## Results

`TimeWarpPage` renders a location trail: Home, then each ancestor page, then the current page.
`PageBreadcrumbTrail` picks the parent as the registered route that is the longest literal prefix.
Ancestors are links. The current crumb is text with `aria-current="page"`. A parameterized page
names its own crumb through `BreadcrumbLabel` (style-guide part title) or through `Title` once the
role name has loaded. `TwBreadcrumb` and `PushRouteInfo` are gone from the shell. `ShowInBreadcrumbs`
false still hides the trail on 403 and 404. Login and logout use the focused page, which has no trail.
The nav uses brand tokens. A trail that is wider than the shell ellipsizes on that one line.

The page source generator is unchanged, so `dev regenerate-generated` left `goldens/ingress` clean.
`TwPageTitle` can still push `RouteState` inside TimeWarp.State.Plus. This repo does not render that
stack and does not call `PushRouteInfo`. No library change.

The command palette has one Style Guide row. The Ctrl-K proof starts on Counter, opens Style Guide,
then clicks Color Tokens. The trail is Home / Style Guide / Color Tokens and does not include Counter.

`ganda repo audit` exited 0. The kanban-landed advisory names other claims (070-001, 104-024,
104-025, 104-026, 123, 141, 198, 244) that have no worktree here. Left as-is.

### How to validate

Smoke: Sign in as a developer. Open `/StyleGuide/color-tokens` directly and reload. From Counter,
press Ctrl-K, choose Style Guide, and click Color Tokens. Click another part, then Color Tokens
again. Open Admin → Roles and open Administrator.

Expect: Every style-guide arrival reads Home / Style Guide / Color Tokens, with one part crumb.
The link color is brand blue (`rgb(0, 133, 178)`) and the current crumb is ink (`rgb(59, 63, 77)`).
The role editor reads Home / Roles / Administrator. A 403 or 404 page shows no breadcrumb.
