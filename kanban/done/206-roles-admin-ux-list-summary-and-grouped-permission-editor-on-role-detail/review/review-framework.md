# Review framework — task 206

**Date:** 2026-09-02
**Host task:** kanban/in-progress/206-roles-admin-ux-list-summary-and-grouped-permission-editor-on-role-detail/
**Diff scope:** branch `task/206-roles-admin-ux-list-summary-and-grouped-permission` vs `origin/master` (`3990f0ac` feat commit; product files under `source/container-apps/web/`)
**Plan / brief:** Task 206 — `/Admin/Roles` list is summary-only (name, description, count/chips); membership editing lives on `RoleDetailPage` (`/Admin/Roles/{RoleId:Guid}`) with prefix-grouped tri-state checkboxes from `PermissionIds.GroupsByPrefix`. Preserve task 182 protected-core + last-admin. COPIC stays `RoleState.SetPermissionSelected` / `SetRolePermissions`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: Grok (2026-09-02) — `ganda task work` review body

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 (re-review after fixes)

**Date:** 2026-09-02
**Scope:** post-fix delta for M1–M3 plus a scan for new defects on that delta.

Fixes applied on this task id:

- M1: `RoleForm` captures `previousCreatedRoleId` and navigates only when `LastCreatedRoleId` changed
- M2: parent `FluentCheckbox` sets `ShowIndeterminate="false"` and `ThreeStateOrderUncheckToIntermediate="true"`
- M3: `AdminPermissions_Should_Match_Every_Admin_Prefix_Id` in `set-role-permissions-tests.cs`

## Product files in scope

- `source/container-apps/web/features/authorization/permission-ids-contracts.cs`
- `source/container-apps/web/features/authorization/permission-ids-tests.cs`
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor` (+ `.cs`, `.css`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor` (+ `.cs`, `.css`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolePage.razor` (+ `.cs`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor` (+ `.css`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor`
- `source/container-apps/web/projects/web-spa/features/admin/roles/role-state/role-state*.cs`
- Design-region-only edits on GetRole / GetRoles contracts and handler

## Task requirements to check

- List has no per-permission checkboxes or per-row Save
- Grouped catalog from `PermissionIds` prefixes (no second SSOT)
- Protected-core Administrator grants cannot be stripped (honest UI disable + server still 409s)
- Last-admin lockout still `SetPrincipalRoles` (unchanged)
- COPIC: `RoleState.SetRolePermissions` / `SetPermissionSelected`
- tw-blazor file order; CSS isolation-first; TWA0004 Design regions reconciled
