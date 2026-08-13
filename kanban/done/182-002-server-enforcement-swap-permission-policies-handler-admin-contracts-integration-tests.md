# Server enforcement swap: permission policies, handler, admin contracts, integration tests

**Parent:** 182 · **Order:** B (after 182-001) · **Depends on:** 182-001 green

## Description

Server evaluates **permissions** via `PermissionRequirement` + `IPermissionEvaluator`. Admin contracts and program.cs leave `RequireRole(Administrator)`. SPA still uses RolePolicyGrants until 182-003 — seed must keep observably equivalent grants.

## Requirements

- Register permission policies from single helper on server.
- Move admin contracts to `PermissionIds.*` policies; split read vs manage endpoints.
- Delete inline RequireRole admin policies in program.cs.
- Integration tests: member 403, admin 200, **read without manage cannot Create/Set**, agent still 401 on admin (scheme).
- api-server out of scope.

## Checklist

- [x] PermissionRequirement + handler registered
- [x] Admin contracts + program.cs
- [x] roles-authorization + principals-authorization tests extended
- [x] Results + How to validate

## Notes

Do not release template to consumers with only B done (SPA still role-based).

## Session

- 2026-08-12: Server enforcement swap — PermissionRequirement + handler, AddPermissionPolicies, 7 admin contracts read/manage split, integration tests, delete RequireRole admin policies.

## Results

### Summary

Server admin APIs enforce **permissions** (not role identity):

- `PermissionRequirement` (contracts) + `PermissionRequirementHandler` (server, via `IPermissionEvaluator` only)
- `PermissionPolicyRegistration.AddPermissionPolicies` registers every `PermissionIds` as a policy
- Admin contracts:
  - GetRoles / GetRole → `admin.roles.read`
  - CreateRole / UpdateRole / DeleteRole → `admin.roles.manage`
  - ListPrincipals → `admin.principals.read`
  - SetPrincipalRoles → `admin.principals.manage`
- `web-server/program.cs`: dropped `RequireRole(Administrator)` CanView* policies; registered permission policies + `IAuthorizationHandler`
- SPA **unchanged** (RolePolicyGrants / CanView* pages until 182-003)
- ModuleRequirement **not** deleted (182-003)
- Integration: Member 403, Admin 200, read-without-manage Create/Set 403, agent 401; Member-only tests force Member after first-admin claim (task 180)

### How to validate

```bash
./bin/dev build   # 0/0

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class RolesAuthorization
dotnet test -c Release -- --filter-class PrincipalsAuthorization

dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs
```

Expect: build 0/0; RolesAuthorization 7/7; PrincipalsAuthorization 6/6; evaluator 11/11.
