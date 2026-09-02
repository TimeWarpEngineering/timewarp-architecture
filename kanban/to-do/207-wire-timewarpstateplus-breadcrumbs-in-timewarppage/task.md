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

- [ ] `TimeWarpPage` renders `TwBreadcrumb` (and title via `TwPageTitle` / `PushRouteInfo`)
- [ ] Role list → detail → crumb back to Roles works
- [ ] "Back to roles" control removed
- [ ] New Role navigates (`FluentAnchorButton`)
- [ ] No Bootstrap CSS added to the template
- [ ] Jaribu / existing roles tests updated if they assert `data-qa="BackToRoles"`

## Session

- Created: ganda session 304250 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)

## Notes

Library follow-up (do not implement in this repo): timewarp-state **081** —
`TwBreadcrumb` must not require Bootstrap CSS. After that ships, bump Plus here if needed and
drop any temporary wrapper CSS.

Working patterns already in this SPA:

- List → detail: `<a href="@RoleDetailPage.GetPageUrl(role.RoleId)">`
- Button then go: `NoSubRouteState.ChangeRoute(...)` (Home, Profile, Counter)
- Sidebar: `TimeWarpNavLink` → `FluentNavItem Href`

v5 link-that-looks-like-a-button: `FluentAnchorButton` (`Href` is a real parameter).
