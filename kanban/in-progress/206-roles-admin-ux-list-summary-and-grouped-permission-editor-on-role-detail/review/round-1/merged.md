# Round 1 — merged findings
**Date:** 2026-09-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor:25-32
- Description: After `await RoleState.CreateRole(Command)`, navigation to detail keys off `RoleState.LastCreatedRoleId is Guid`. That id is only assigned in `CreateRole` `HandleSuccess` and is never cleared at the start of a new create. `DefaultApiHandler.HandleError` toasts `SharedProblemDetails` and does not throw, so a failed create leaves the previous successful id in place and this form navigates to the wrong role.
- Suggestion: Capture `Guid? before = RoleState.LastCreatedRoleId` before dispatch and navigate to detail only when the value changed; on failure stay on New Role so the toast is visible.
- Source: general
- Disposition notes: Capture `previousCreatedRoleId` before dispatch; navigate only when `LastCreatedRoleId` changed. Failed create stays on New Role.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor:79-86
- Description: Parent `FluentCheckbox` uses `ThreeState="true"` with FluentUI v5 defaults (`ShowIndeterminate=true`, `ThreeStateOrderUncheckToIntermediate=false`). Click order is Unchecked→Checked→Intermediate and Intermediate→Unchecked. `OnGroupCheckStateChanged` maps only `checkState == true` to select (null and false both clear), so a mixed parent click clears unlocked children instead of selecting the rest.
- Suggestion: Set `ShowIndeterminate="false"` and `ThreeStateOrderUncheckToIntermediate="true"` so mixed→checked (select all unlocked), checked→unchecked (clear), unchecked→checked, while indeterminate remains display-only from `CheckStateFor`.
- Source: general
- Disposition notes: Parent checkbox now sets both flags; Design region records the FluentUI v5 click order.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/authorization/permission-ids-contracts.cs:103-108
- Description: SPA lock is `Administrator && Prefix == "admin"`; server `AdminLockoutGuards.ProtectedCoreConflict` requires every `RolePermissionSeed.AdminPermissions` id. Today those sets are identical. The coupling is documentary only; a later `admin.*` added to `All` but not `AdminPermissions` (or the reverse) would silently diverge.
- Suggestion: Add an application-layer assertion that `RolePermissionSeed.AdminPermissions` equals every `PermissionIds.All` id whose `Prefix` is `"admin"`. Do not reference application seed from contracts.
- Source: general
- Disposition notes: `AdminLockoutGuards_ProtectedCore_Given_.AdminPermissions_Should_Match_Every_Admin_Prefix_Id` pins the two sets; Design region notes contracts cannot reference the seed.

## Duplicates / conflicts

- None. Three independent findings.
