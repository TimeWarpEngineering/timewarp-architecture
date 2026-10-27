# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** branch vs `c223df1cc`: `TimeWarpPage.razor` and `.razor.css`, `page-breadcrumb-trail.cs`,
`StyleGuidePage.razor`, the role page comments, the `tw-blazor-layout` and `tw-blazor-css-strategy` skills, and the three test files.

## Summary

The change replaces the `RouteState` history trail with a location trail.
`PageBreadcrumbTrail.Build` reads `PageRegistry.All` at run time. A parent is the longest strict
literal prefix on a segment boundary, and candidates that contain `{` are skipped. The result is
Home, then the ancestors, then the current page. `TimeWarpPage` renders it as a native
`<nav aria-label="Breadcrumb"><ol>` with isolated token rules. `TwBreadcrumb` and `PushRouteInfo`
are gone (grep finds no reference in `source/`, `tests/` or `skills/`). `ShowInBreadcrumbs=false` still
omits the trail. Other `RouteState.ChangeRoute` callers are unchanged.

Every requirement in the task is met, and the implementer restatement matches the task. No misread.

Gates I re-ran in this worktree:
- `./bin/dev build`: 0 warnings, 0 errors.
- `web-spa-integration-tests`: 519 of 519 passed, including `PageBreadcrumbTrail_Should_`.
- `web-spa-playwright-tests --filter-class Breadcrumb_Given_Wasm`: 1 of 1 passed. That test covers
  a direct URL plus reload, part-to-part clicks, Counter → Ctrl-K → Style Guide → part, the role
  editor, and the computed link color, current color and margin.
- `goldens/` has no diff against the base.
- `./bin/dev lint-skills`: the client-name scan passed. The vally spec lint did not run because vally
  is not on PATH on this machine. CI's skill-lint job is the remaining check.

Notes checked and not raised as findings:
- `PageRegistry.All` lists only navigable pages, because the generator requires `Navigable` and a
  static primary route. A non-navigable literal page, such as `/Admin/Roles/New`, is never an
  ancestor. As the current page it falls back to its `Title` ("Home / Roles / <title>"). The task
  defines the registry as `PageRegistryEntry`, so this matches the spec.
- The Ctrl-K case opens Style Guide from Counter and then clicks a part, because the palette has
  no row for each part. That reads the requirement as intended. It proves that history does not leak.
- The trail rebuilds on each `TimeWarpPage` render. A route change always changes either the page
  type or a parameter (`Part`, `RoleId` → `Title`), so the trail does not go stale.
- Unverified observation, left for a human: `.twe-breadcrumb` has `overflow: hidden` (for the
  ellipsis) and no padding. A keyboard focus ring on the first link may lose its left edge to the
  clip. Not measured, so not filed.

## Issues

None.
