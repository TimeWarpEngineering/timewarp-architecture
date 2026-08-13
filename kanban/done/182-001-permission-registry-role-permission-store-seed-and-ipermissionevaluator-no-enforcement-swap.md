# Permission registry, role-permission store, seed, and IPermissionEvaluator (no enforcement swap)

**Parent:** 182 · **Order:** A (first) · **Enforcement:** RequireRole still live

## Description

Stand up the permission model and grant expansion with **zero user-visible enforcement change**. After this child, RequireRole / RolePolicyGrants still gate surfaces; new store and evaluator are tested in isolation.

## Requirements

- Permission registry: dotted lowercase `const string` ids (`admin.roles.read`, `admin.roles.manage`, `admin.principals.read`, `admin.principals.manage`, `admin.access`, `developer.access`, `developer.claims.read`, `profile.read`, `settings.read`, … per disposition seed table).
- Role→permission grant store: dual-mode in-memory + EF (mirror principal-role store); seed Administrator / Member / Developer / Operator (Operator may be empty reserved).
- `IPermissionEvaluator` + default impl (principal → roles → permissions); scheme-aware so agents do not inherit human expansion.
- Co-located Jaribu tests: empty, seed expand, no admin on Member, read≠manage sets differ.
- Draft ADR notes alongside (accept in 182-005).
- **Do not** change `[EndpointAuthorize]`, SPA policies, or program.cs RequireRole.

## Checklist

- [x] Registry + single registration helper skeleton (may not yet replace both constant classes until C)
- [x] Role-permission store + seed
- [x] IPermissionEvaluator + tests
- [x] ADR draft started under documentation or task folder
- [x] `dev build` 0/0; Results + How to validate

## Notes

Disposition: `kanban/in-progress/182-…/disposition.md`. Round-2: `review/round-2/grok.md`.

## Session

- 2026-08-12: Implementation — registry, dual-mode role→permission store, seed, evaluator, migration, ADR draft, Jaribu tests. No enforcement swap.

## Results

### Summary

Permission model is live with **zero user-visible enforcement change**:

- `PermissionIds` registry (dotted strings) + `PermissionPolicyRegistration.AllPermissionPolicyNames` skeleton
- `IRolePermissionStore` dual-mode: `InMemoryRolePermissionStore` (seeded singleton) / `EfRolePermissionStore` (scoped when Postgres connected)
- `RolePermissionSeed.DefaultGrants` for Administrator / Member / Developer / Operator
- `IPermissionEvaluator` / `PermissionEvaluator` — scheme-aware expansion via `IEffectiveRolesResolver` + grant store
- EF: `identity.role_permissions` + migration seed data
- ADR draft: `documentation/.../proposed/0010-permission-centric-authorization.md`
- RequireRole / RolePolicyGrants / `[EndpointAuthorize]` / ModuleIds untouched

### How to validate

```bash
# Full solution (0 warnings / 0 errors)
./bin/dev build

# Co-located Jaribu (standalone)
dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs

# Family aggregator (MTP)
cd tests/container-apps/web/web-jaribu-tests && \
  dotnet test -c Release -- --filter-class PermissionEvaluator
```

Expect: 11 tests pass (9 evaluator + 2 store); build 0/0. Confirm program.cs still registers RequireRole policies and no PermissionRequirement handler yet.
