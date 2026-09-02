# Wire TimeWarp.State.Plus breadcrumbs in TimeWarpPage

## Description

Role detail's "Back to roles" does nothing. It is a v5 `FluentButton` with `Href` — Fluent UI
Blazor 5 `FluentButton` has **no `Href`** (only `OnClick`). The unmatched attribute is splat onto
`<fluent-button href="…">`, which does not navigate. **New Role** on the list is the same dead
pattern.

Do **not** keep a per-page Back button. Navigation trail belongs in the shell, the way COPIC
puts crumbs in `MainContentArea` from `RouteState` — but consume **TimeWarp.State.Plus**
(`TwBreadcrumb` + `TwPageTitle`), not COPIC's v4 `FluentBreadcrumb`.

This template already references Plus `12.0.0-beta.1` and has `RouteState` / `NoSubRouteState`
on `BaseComponent`. Pages use raw `<PageTitle>`, so the route stack is not fed. `TimeWarpPage`
is an `<h1>` only.

**Do not add Bootstrap** to this template.

## Requirements

- Put the trail in **`TimeWarpPage`** (once), under the page title. Not on `RoleDetailPage`.
- Feed `RouteState` via **`TwPageTitle`** (or equivalent `PushRouteInfo` from the shell). Pages
  that set a title should go through that so crumbs get `PageTitle` text.
- Render **`<TwBreadcrumb />`** from Plus. Do **not** vendor-copy COPIC. Do **not** add
  `bootstrap.min.css` or Bootstrap classes as a styling system.
- Plus `TwBreadcrumb` currently emits Bootstrap class names (`breadcrumb`, `breadcrumb-item`,
  `text-muted`) with **no** CSS of its own (timewarp-state **081**). Until Plus ships isolated
  CSS, a thin isolated wrapper on `TimeWarpPage` may style the trail so it is readable. Do not
  load Bootstrap. Do not restyle the whole app as Bootstrap.
- Remove the "Back to roles" `FluentButton` from `RoleDetailPage`.
- **New Role**: `FluentAnchorButton` (v5 link-button with real `Href`) to `RolePage.GetPageUrl()`,
  not `FluentButton Href`.
- Other `FluentButton Href` in this SPA (if any) follow the same rule: `FluentAnchorButton` or
  `OnClick` + `ChangeRoute`.
- Auth/403/404: do not pollute the trail if COPIC's `ShowInBreadcrumbs=false` pattern is needed;
  only if those pages currently would push junk.

## Checklist

- [x] `TimeWarpPage` renders `TwBreadcrumb` (and title via `TwPageTitle` / `PushRouteInfo`)
- [x] Role list → detail → crumb back to Roles works
- [x] "Back to roles" control removed
- [x] New Role navigates (`FluentAnchorButton`)
- [x] No Bootstrap CSS added to the template
- [x] Jaribu / existing roles tests updated if they assert `data-qa="BackToRoles"`

## Session

- Created: ganda session 304250 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Implementer: grok (2026-09-02)

## Results

Wired Plus breadcrumbs into the SPA shell and replaced the dead v5 `FluentButton Href` patterns.

**What shipped**

- `TimeWarpPage` owns the trail once: `TwPageTitle` (when `Title` is set and `ShowInBreadcrumbs`) plus `TwBreadcrumb MaxLinks="4"` under the `h1`. Interactive `OnAfterRenderAsync` also `PushRouteInfo` so async titles (role name after `FetchRoles`) update the current stack entry. `PushRouteInfo` is skipped when `!RendererInfo.IsInteractive` (prerender has no `document.title` JS).
- `ShowInBreadcrumbs=false` on Forbidden, Not Found, and Authentication — those pages still set `PageTitle` but do not push onto `RouteState`. Login/Logout stay on `TimeWarpFocusedPage` with raw `PageTitle` (no trail).
- `RoleDetailPage`: removed `data-qa="BackToRoles"` `FluentButton`.
- `RolesListPage`: New Role is `FluentAnchorButton` to `RolePage.GetPageUrl()`.
- No other `FluentButton Href` in this SPA.
- Pages that already passed `Title` into `TimeWarpPage` dropped duplicate `<PageTitle>` so crumbs and the document title share one string.
- Thin `.twe-page__crumbs` CSS in the existing `.twe-shell` style block styles Plus's Bootstrap class names with tokens. No `bootstrap.min.css`.

**Files**

- `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`
- Role list/detail/new pages; Forbidden/NotFound/Authentication; Counter, Chat, Event Stream, Passkeys, Principals (dropped extra `PageTitle`)
- `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs`

**Decisions**

- Shell `PushRouteInfo` in addition to `TwPageTitle`: Plus `TwPageTitle` only pushes on its first render; role detail title is data-dependent.
- Wrapper CSS stays until timewarp-state **081** ships isolated `TwBreadcrumb` CSS.

**Tests**

- `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release` — 0/0
- `dotnet build source/container-apps/web/projects/web-server/web-server.csproj -c Release` — 0/0
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Ok_Page_Given_Passkey_Administrator_Admin_Roles_Html` — passed (prerender HTML has `data-qa="NewRole"`, `aria-label="breadcrumb"`, no `BackToRoles`)

Interactive crumb `GoBack` (Roles → role → click Roles in the trail) was not driven in a live browser in this session; that path is Plus `RouteState.GoBack` plus the wrapper CSS. Manual smoke below.

### How to validate

**Automated**

```bash
dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release
# expect: 0 Warning(s), 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Ok_Page_Given_Passkey_Administrator_Admin_Roles_Html
# expect: passed; prerendered /Admin/Roles HTML contains data-qa="NewRole" and aria-label="breadcrumb", not data-qa="BackToRoles"
```

**Smoke**

1. `./bin/dev run` (Development; mock or passkey admin so `/Admin/Roles` is allowed).
2. Open Admin → Roles. Confirm a trail under the Roles heading (`nav[aria-label=breadcrumb]`), no Bootstrap stylesheet, and **New Role** is a real link (`fluent-anchor-button` / `<a>`) to `/Admin/Roles/New`.
3. Click a role name. Confirm the "Back to roles" button is gone. Confirm the trail shows Roles then the role name.
4. Click **Roles** in the trail. Expect the list again (`data-qa="RolesTable"`).

**Expect**

- New Role navigates to `/Admin/Roles/New`.
- Role detail has no `data-qa="BackToRoles"` control.
- Crumb ancestor uses `RouteState.GoBack`, not a dead `FluentButton Href`.
- 403/404/auth do not add a junk crumb (`ShowInBreadcrumbs=false`).
- No `bootstrap.min.css` (or Bootstrap as a styling system) in the template.

**Depends on:** signed-in principal with `admin.roles.read` (and `admin.roles.manage` for New Role).

**Not in scope:** timewarp-state **081** (Plus `TwBreadcrumb` isolated CSS). Drop the `.twe-page__crumbs` Bootstrap-class wrapper after that bump.

## Notes

Library follow-up (do not implement in this repo): timewarp-state **081** —
`TwBreadcrumb` must not require Bootstrap CSS. After that ships, bump Plus here if needed and
drop any temporary wrapper CSS.

Working patterns already in this SPA:

- List → detail: `<a href="@RoleDetailPage.GetPageUrl(role.RoleId)">`
- Button then go: `NoSubRouteState.ChangeRoute(...)` (Home, Profile, Counter)
- Sidebar: `TimeWarpNavLink` → `FluentNavItem Href`

v5 link-that-looks-like-a-button: `FluentAnchorButton` (`Href` is a real parameter).
