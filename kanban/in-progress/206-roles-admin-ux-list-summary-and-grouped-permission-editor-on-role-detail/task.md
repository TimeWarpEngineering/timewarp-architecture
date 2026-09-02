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

- [x] List page: summary only (name, description, count/chips); New Role stays
- [x] Role detail/edit: grouped permission editor (prefix parents)
- [x] Protected-core + last-admin lockout still hold (server + honest UI disable)
- [x] Design regions on touched razor/state files
- [x] `dev build` 0/0; lockout/auth tests green

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
- Implementer: Grok (2026-09-02) — list summary + grouped RoleDetailPage; protected-core UI lock
- Review oracle: Grok (2026-09-02) — effort 1 general; `review/` under this folder
- Review round 1: general — M1 bug + M2/M3 suggestions; fixed on this id
- Review round 2: general — M1–M3 re-verified fixed; disposition clean

## Results

List is a list again. `/Admin/Roles` shows name (link), description, grant count, and prefix chips (`admin.*`, `developer.*`, …). Per-permission checkboxes and per-row Save are gone.

Membership editing lives on **`RoleDetailPage`** at `/Admin/Roles/{RoleId:Guid}`: `PermissionIds.GroupsByPrefix` parent checkboxes (tri-state) plus atom ids. COPIC is unchanged — `RoleState.SetPermissionSelected` / `SetRolePermissions`. `RolePage` (`/Admin/Roles/New`) stays create-only; after a successful create, `RoleForm` navigates to the new role’s detail page (only when `LastCreatedRoleId` changes; a failed create stays on New Role).

Protected-core: selected `admin.*` grants on Administrator are disabled in the editor (`PermissionIds.IsProtectedCoreLocked`). Missing cores stay togglable so a damaged bundle can be repaired. Server `SetRolePermissions` still 409s a strip. Last-admin remains `SetPrincipalRoles` (unchanged).

Packs were not added — prefix groups cover the current catalog.

**Files**

- `source/container-apps/web/features/authorization/permission-ids-contracts.cs` — `Prefix`, `GroupsByPrefix`, `PrefixesOf`, protected-core helpers
- `source/container-apps/web/features/authorization/permission-ids-tests.cs` — host-free grouping + lock tests
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor` (+ `.css`, `.razor.cs`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor` (+ `.css`, `.razor.cs`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor` (+ `.css`)
- `RoleForm.razor`, `RolePage.razor(.cs)`, `role-state*.cs`, GetRole/GetRoles Design regions

**Tests (this session)**

- `dotnet run source/container-apps/web/features/authorization/permission-ids-tests.cs` — 8 passed
- `dotnet run source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs` — 13 passed (protected-core + last-admin unit + admin.* vs AdminPermissions pin)
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SetRolePermissionsLockout` — 3 passed
- same suite `--filter-class RolesAuthorization` — 7 passed
- same suite `--filter-class PrincipalsAuthorization` — 8 passed (includes last-admin)
- same suite `--filter-class ProtectedPage` — 7 passed (`/Admin/Roles` prerender still 200 for admin)
- `dotnet run tools/dev-cli/dev.cs -- build` — 0 warning / 0 error

Live Aspire/browser click-through was not run (no orchestrator up in this worktree). Closest UI proof: SPA Release/Debug compile, list markup has zero `FluentCheckbox`, detail editor is the only membership surface.

### How to validate

**Automated**

```bash
dotnet run source/container-apps/web/features/authorization/permission-ids-tests.cs
# expect: 8 passed (every All id in exactly one prefix group; Administrator + selected admin.* locks)

dotnet run source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs
# expect: 13 passed, including ProtectedCoreConflict 409, LastAdministratorConflict 409,
# and AdminPermissions matching every admin.* catalog id

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SetRolePermissionsLockout
# expect: 3 passed — stripping admin.roles.manage from Administrator is HTTP 409; Member write is 200

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class PrincipalsAuthorization
# expect: last-admin demotion 409 still holds

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Manual smoke**

1. `dotnet run tools/dev-cli/dev.cs -- run` (or `./bin/dev run` if installed). Sign in as an Administrator.
2. Open `/Admin/Roles`.
3. **Expect:** table columns Name / Description / Permissions; Permissions is a count plus chips like `admin.*` — no checkboxes, no Save on the row. **New Role** still present.
4. Click **Administrator**. **Expect:** `/Admin/Roles/{administrator-guid}` with grouped checkboxes (`admin.*` parent + atoms). The selected `admin.*` atoms and the `admin.*` parent are disabled. `profile.read` / `settings.read` stay enabled.
5. Click **Member**, grant `developer.access`, Save. **Expect:** success; returning to the list shows a `developer.*` chip on Member.
6. (API) PUT Administrator permissions without `admin.roles.manage`. **Expect:** 409 problem+json title `Protected core permissions`.
7. Create a role successfully, then submit New Role with a name that the API rejects (or force a problem+json). **Expect:** stay on `/Admin/Roles/New` (toast); do not jump to the previously created role.

**Depends on:** identity-session (passkey) or Development mock auth for the SPA; integration tests mint a passkey cookie themselves.

**Not in scope:** permission packs, Principals-page role checkboxes, OpenFGA/Cedar, renaming permission ids, Playwright e2e.

### Review disposition

**Outcome:** clean (0 open). **Effort:** 1 (general only). **Rounds:** 2.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

- M1 (bug, fixed): `RoleForm` no longer treats a sticky `LastCreatedRoleId` as success after a toasted create failure.
- M2 (suggestion, fixed): parent `FluentCheckbox` uses `ShowIndeterminate=false` and `ThreeStateOrderUncheckToIntermediate=true` so mixed→select-all, checked→clear.
- M3 (suggestion, fixed): application test pins `RolePermissionSeed.AdminPermissions` to every `admin.*` id in `PermissionIds.All`.

**Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.
