# Roles admin UX: list summary and grouped permission editor on role detail

## Description

`/Admin/Roles` (`RolesListPage.razor`) puts **every** id in `PermissionIds.All` as a
`FluentCheckbox` inside each table row (wrapping `FluentStack`, raw dotted labels like
`admin.roles.manage`). Today that is ~12 permissions × 4 product roles. It already looks
like a prototype; it will not survive marketplace (**118**) or more agent/self-service ids.

Task **182** shipped the model (roles = permission bundles, protected-core on Administrator,
last-admin lockout). That teaching surface is **not** the durable admin UX. Do not grow more
checkboxes onto the list table.

## Target UX

1. **List is a list** — name, description, a short summary (count and/or group chips). No
   per-permission widgets. Save does not live on every row.
2. **Edit membership on a role page** — `RolePage` today is create-only (`RoleForm` name +
   description). Membership belongs on a role **detail/edit** route, not in the grid.
3. **Group by prefix** (`admin.*`, `developer.*`, `credential.*`, …) with a parent checkbox /
   tri-state, not a flat catalog of atoms.
4. **Packs for common roles** (optional, if grouping is not enough) — e.g. “Admin surface”,
   “self-service”, “metered demo” — with an advanced picker for leftovers.

A full roles×permissions matrix still dies around a few dozen ids. Searchable dual-list or
grouped tree on the **detail** page is the durable shape.

## Requirements

- `/Admin/Roles` list: no per-permission checkboxes in table cells
- Permission membership editor on role detail/edit (reuse or extend `RolePage`)
- Grouped catalog from `PermissionIds` prefixes (do not invent a second permission SSOT)
- Preserve **182** behavior: protected-core Administrator grants cannot be stripped; last
  Administrator lockout still enforced server-side (`SetRolePermissions`)
- COPIC: still `RoleState.SetRolePermissions` / `SetPermissionSelected` — this is SPA chrome,
  not a new store
- `dev build` 0/0; existing roles authorization / lockout tests still green; add UI-level
  coverage if the list/detail split is testable without Playwright-only

## Checklist

- [ ] List page: summary only (name, description, count/chips); New Role stays
- [ ] Role detail/edit: grouped permission editor (prefix parents)
- [ ] Protected-core + last-admin lockout still hold (server + honest UI disable)
- [ ] Design regions on touched razor/state files
- [ ] `dev build` 0/0; lockout/auth tests green

## Notes

- Call site today: `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor`
- Catalog: `PermissionIds.All` in `source/container-apps/web/features/authorization/permission-ids-contracts.cs`
- Related: **182** (permission-centric auth — keep the model), **118** (marketplace will grow
  the catalog), **132** (auth folder naming — not this UX)
- Not in scope: OpenFGA/Cedar, renaming permission ids, Principals page role checkboxes
  (separate list; may follow the same pattern later)

## Session

- Created: 73480 (2026-09-02)
- Cockpit: Grok — operator: `/Admin/Roles` checkbox soup will not scale
- Cockpit: Grok launch (2026-09-02) — claim, in-progress, `ganda task work`

Stay in this claim worktree. Implement the checklist (list summary + grouped editor on
role detail). Preserve 182 protected-core / last-admin lockout. Results + How to validate,
`ganda kanban done 206`, PR, STOP. Do not merge.
