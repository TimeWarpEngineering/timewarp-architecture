# Round 2 — merged findings
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
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor:27-32
- Description: `HandleValidSubmit` captures `previousCreatedRoleId` before `CreateRole` and navigates only when `LastCreatedRoleId` changed. A toasted failure no longer routes to a prior role.
- Suggestion: Compare to pre-dispatch snapshot; stay on New Role on failure.
- Source: general
- Disposition notes: Re-verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RolePermissionEditor.razor:82-88
- Description: Parent checkbox flags match FluentUI v5 click order (mixed→checked, checked→unchecked, unchecked→checked); indeterminate is display-only.
- Suggestion: `ShowIndeterminate=false` and `ThreeStateOrderUncheckToIntermediate=true`.
- Source: general
- Disposition notes: Re-verified in round 2 against FluentCheckbox state machine.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs:175-187
- Description: Application-layer pin that `RolePermissionSeed.AdminPermissions` equals every `admin.*` id in `PermissionIds.All`.
- Suggestion: Keep contracts prefix-based; pin equality in application tests.
- Source: general
- Disposition notes: Re-verified in round 2. Tests: 13 passed.

## Duplicates / conflicts

- None. Prior M# IDs carried forward; no new findings.
