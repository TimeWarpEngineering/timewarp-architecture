# Round 2 — general
**Date:** 2026-09-02
**Scope reviewed:** post-fix delta for M1–M3 (RoleForm, RolePermissionEditor, set-role-permissions-tests, permission-ids-contracts Design)

## Summary

Re-verified M1–M3 against the working-tree fix delta. `RoleForm` now compares `LastCreatedRoleId` to a pre-dispatch snapshot so failed creates stay on New Role; parent `FluentCheckbox` flags match FluentUI `5.0.0-rc.5-26219.1` click semantics (mixed→checked, checked→unchecked, unchecked→checked); and `AdminPermissions_Should_Match_Every_Admin_Prefix_Id` pins seed vs every `admin.*` catalog id in `All` order. No new defects on the fix delta.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor:27-32
- Description: `HandleValidSubmit` captures `Guid? previousCreatedRoleId = RoleState.LastCreatedRoleId` before `CreateRole`, then navigates only when `LastCreatedRoleId is Guid roleId && roleId != previousCreatedRoleId`. Sticky success id after a toasted failure no longer routes to the wrong role; Design region documents the sticky-id constraint. List fallback on unset id was removed so failure stays on the form.
- Suggestion: Capture `previousCreatedRoleId` before dispatch; navigate only when `LastCreatedRoleId` changed.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor:82-88
- Description: Parent checkbox now sets `ThreeState="true"`, `ShowIndeterminate="false"`, and `ThreeStateOrderUncheckToIntermediate="true"`. Against the pinned FluentUI package state machine (`OnCheckChangedHandlerAsync`), that yields mixed→`SetToCheckedAsync`, checked→`SetToUncheckedAsync`, unchecked→`SetToCheckedAsync`; indeterminate remains display-only via `CheckStateFor` / `CheckState`. `OnGroupCheckStateChanged` still maps `checkState == true` to select and otherwise clears unlocked children — now aligned with those clicks.
- Suggestion: Set both flags so parent clicks select-all / clear as intended.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs:175-187
- Description: `AdminLockoutGuards_ProtectedCore_Given_.AdminPermissions_Should_Match_Every_Admin_Prefix_Id` builds every `PermissionIds.All` id with `Prefix == "admin"` and asserts equality with `RolePermissionSeed.AdminPermissions` (same order as `All`). Contracts Design region notes the pin and that contracts must not reference the application seed.
- Suggestion: Application-layer pin that the two sets stay identical.
- Status: fixed

## Issues
