# Review framework — task 207

**Date:** 2026-09-02
**Host task:** kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/
**Diff scope:** branch `task/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage` vs `origin/master` (feat `3f6e0bf4`; product files under `source/container-apps/web/projects/web-spa/` plus one web-server-integration test)
**Plan / brief:** Task 207 — Role detail "Back to roles" and list "New Role" were dead v5 `FluentButton Href`. Put the trail once in `TimeWarpPage` via Plus `TwPageTitle` + `TwBreadcrumb` (feed `RouteState`); remove the per-page Back button; use `FluentAnchorButton` for New Role. No Bootstrap. 403/404/auth must not pollute the trail.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: Grok (2026-09-02) — `ganda task work` review body

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor`
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor`
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolePage.razor`
- `source/container-apps/web/projects/web-spa/features/application/pages/ForbiddenPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/pages/NotFoundPage.razor`
- `source/container-apps/web/projects/web-spa/pages/Authentication.razor`
- Duplicate `<PageTitle>` dropped on Principals, Chat, Counter, Event Stream, Passkeys
- `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs`

## Task requirements to check

- Trail lives in `TimeWarpPage` once (under the page title), not on `RoleDetailPage`
- `RouteState` is fed via `TwPageTitle` and/or shell `PushRouteInfo`; pages that set a title go through that
- Render Plus `<TwBreadcrumb />` — do not vendor-copy COPIC; do not add `bootstrap.min.css` or Bootstrap as a styling system
- Thin isolated wrapper CSS on `TimeWarpPage` may style Plus Bootstrap class names with tokens
- Remove "Back to roles" `FluentButton` from `RoleDetailPage`
- New Role is `FluentAnchorButton` with real `Href` to `RolePage.GetPageUrl()`
- Other `FluentButton Href` in this SPA follow the same rule
- Auth/403/404 do not pollute the trail (`ShowInBreadcrumbs=false` or equivalent)
- Login/Logout stay on `TimeWarpFocusedPage` with raw `PageTitle` (no trail) — in scope only to confirm they were not wrongly converted
- tw-blazor file order; CSS isolation-first / Exception B shell styles; Design comments reconciled
- Tests: if they asserted `data-qa="BackToRoles"`, they were updated

## Plus APIs (package pin `TimeWarp.State.Plus` 12.0.0-beta.1)

- `TwPageTitle` — XML: a component that Sends `RouteState.PushRouteInfo.Action` for every `OnAfterRenderAsync` is `TimeWarpPageRenderNotifier`; confirm `TwPageTitle` vs shell `PushRouteInfo` interaction (duplicate stack entries vs in-place update)
- `TwBreadcrumb.MaxLinks` / `NavigateBack`
- `RouteState.PushRouteInfo` reads `document.title` via JS — prerender skip is claimed
- Package XML at `~/.nuget/packages/timewarp.state.plus/12.0.0-beta.1/lib/net8.0/TimeWarp.State.Plus.xml`
- DLL: `~/.nuget/packages/timewarp.state.plus/12.0.0-beta.1/lib/net8.0/TimeWarp.State.Plus.dll`

## Implementer claims to re-verify

- Interactive crumb `GoBack` was **not** driven in a live browser; prerender HTML asserts `aria-label="breadcrumb"` and no `BackToRoles`
- `TwPageTitle` only pushes on first render; shell also `PushRouteInfo` when interactive so async titles (role name after `FetchRoles`) update the current stack entry
- Wrapper CSS stays until timewarp-state **081**
