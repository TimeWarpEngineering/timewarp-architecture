# Roles UI permission membership editor with last-admin and protected-core lockout guards

**Parent:** 182 · **Order:** D (after 182-003) · **Depends on:** 182-001–003

## Description

Admin can edit which permissions a role includes. **Must not ship without lockout guards** (disposition blocking #5).

## Requirements

- Roles UI: multi-select / matrix from permission registry for role membership.
- Optional read-only Permissions catalog page gated `admin.*`.
- **Last-admin:** SetPrincipalRoles cannot remove the last principal who holds a role granting `admin.principals.manage` (409).
- **Protected-core:** Administrator (or last role granting core admin permissions) cannot have core `admin.*` stripped — prefer system-role rule.
- Tests for both lockouts (resource-check teaching exemplars).

## Checklist

- [x] UI membership editor
- [x] Last-admin guard + tests
- [x] Protected-core guard + tests
- [x] Results + How to validate

## Results

### What shipped

1. **`SetRolePermissions`** contract + handler (`PUT api/Roles/{RoleId}/permissions`)
   - Policy `PermissionIds.AdminRolesManage`; schemes identity-session + mock-identity-session
   - Validator: each id ∈ `PermissionIds.All`; RoleId not empty
   - Handler: 404 unknown role; **protected-core** on `RoleIds.Administrator` via `AdminLockoutGuards.ProtectedCoreConflict` (must retain all `RolePermissionSeed.AdminPermissions`); then dumb store write; echo stored list

2. **GetRole / GetRoles** extended with `PermissionIds` filled from `IRolePermissionStore`

3. **Last-admin** on `SetPrincipalRoles` before write: count principals whose effective roles grant `admin.principals.manage`; if sole admin is the target and proposed roles would drop that permission → 409

4. **`AdminLockoutGuards`** (application helper): protected-core, last-admin problem factory, role→permission expansion, effective-role simulation (Member default + bootstrap)

5. **SPA RolesListPage**: permission multi-select matrix (`PermissionIds.All` checkboxes per role) + Save → `SetRolePermissions`; drafts in `RoleState`

6. **Skipped** optional read-only Permissions catalog page (nice-to-have)

### Tests

| Coverage | Location |
|----------|----------|
| Protected-core unit + SetRolePermissions contracts | `set-role-permissions-tests.cs` (12 green) |
| Last-admin unit helpers | same runfile |
| Protected-core HTTP 409 / Member OK / Admin keep-core OK | `set-role-permissions-lockout-tests.cs` (3 green) |
| Last-admin 409 / demote-when-two-admins 200 | `principals-authorization-tests.cs` (+ fixed demote-only-admin with peer) |
| RoleDto/GetRole/SetRolePermissions serialization | `role-contracts-serialization-tests.cs` |

### How to validate

```bash
export PATH="$PWD/bin:$PATH"
dev build   # 0/0

dotnet run source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Lockout
dotnet test -c Release -- --filter-class PrincipalsAuthorization
dotnet test -c Release -- --filter-class RolesAuthorization

cd ../web-contracts-tests
dotnet test -c Release -- --filter-class RoleContracts
```

Manual: sign in as Administrator → Admin/Roles → toggle permissions → Save; stripping any core admin.* from Administrator returns problem+json 409; Principals demoting last admin likewise 409.

## Session

- Implemented 182-004 fully: SetRolePermissions, GetRole/GetRoles PermissionIds, AdminLockoutGuards, SetPrincipalRoles last-admin, RolesListPage matrix UI, integration + co-located tests.
- Parent disposition: lockout guards required with membership editing — both guards landed before UI ship.
- Optional Permissions catalog page deferred.
