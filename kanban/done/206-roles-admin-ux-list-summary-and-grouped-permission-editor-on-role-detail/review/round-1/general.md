# Round 1 — general
**Date:** 2026-09-02
**Scope reviewed:** branch task/206-roles-admin-ux-list-summary-and-grouped-permission vs origin/master (source/)

## Summary

Task 206 cleanly splits `/Admin/Roles` into a summary list (count + prefix chips, no membership widgets) and a Guid-routed `RoleDetailPage` with `PermissionIds.GroupsByPrefix` tri-state parents plus atom checkboxes, still driving COPIC `SetPermissionSelected` / `SetRolePermissions`. Protected-core UI lock currently matches server `RolePermissionSeed.AdminPermissions` (all `admin.*` in `All`), CanManage honestly disables Save/tree, routes/`[Page]` policies look correct, and host-free grouping/lock tests are appropriate. Main risk is post-create navigation treating a stale `LastCreatedRoleId` as success when CreateRole only toasts on API failure.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor:23-32
- Description: After `await RoleState.CreateRole(Command)`, navigation to detail keys off `RoleState.LastCreatedRoleId is Guid`. That id is only assigned in `CreateRole` `HandleSuccess` and is never cleared at the start of a new create (`role-state.create-role.cs:50`, cleared only in `RoleState.Initialize`). `DefaultApiHandler.HandleError` toasts `SharedProblemDetails` and does not throw (`default-api-handler.cs:37-40`), so a failed create (network / 403 / problem+json) leaves the previous successful id in place and this form navigates to the wrong role instead of staying put or falling back to the list.
- Suggestion: Capture `Guid? before = RoleState.LastCreatedRoleId` before dispatch and navigate only when the value changed; or clear `LastCreatedRoleId` at the start of CreateRole and treat a still-null id after await as failure (keep the user on New / list).
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor:79-84
- Description: Parent `FluentCheckbox` uses `ThreeState="true"` with FluentUI v5 defaults (`ShowIndeterminate=true`, `ThreeStateOrderUncheckToIntermediate=false`). Per `FluentCheckbox.razor.cs` click order that means Unchecked→Checked→Intermediate and Intermediate→Unchecked. `OnGroupCheckStateChanged` maps only `checkState == true` to select (null and false both clear), so a mixed parent click clears unlocked children instead of selecting the rest — atypical for a tree parent, and Checked→null relies on treating indeterminate as “clear.”
- Suggestion: For parent-reflects-children semantics set `ShowIndeterminate="false"` and `ThreeStateOrderUncheckToIntermediate="true"` so mixed→checked (select all unlocked), checked→unchecked (clear), unchecked→checked, while indeterminate remains display-only from `CheckStateFor`. Keep skipping `IsPermissionDisabled` children as today.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/features/authorization/permission-ids-contracts.cs:103-108
- Description: SPA lock is `Administrator && Prefix == "admin"`; server `AdminLockoutGuards.ProtectedCoreConflict` requires every `RolePermissionSeed.AdminPermissions` id. Today those sets are identical (five `admin.*` ids), so the UI will not allow a strip the server 409s, nor lock something the server ignores. The coupling is documentary only (`Design` claims they match); a later `admin.*` added to `All` but not `AdminPermissions` (or the reverse) would silently diverge.
- Suggestion: Add an application-layer assertion (e.g. in `set-role-permissions-tests.cs`) that `RolePermissionSeed.AdminPermissions` equals every `PermissionIds.All` id whose `Prefix` is `"admin"`, or share one list so UI lock and 409 stay the same SSOT.
- Status: open
