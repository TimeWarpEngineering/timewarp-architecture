# Trim RoleIds to product roles and map role-to-policy grants

## Parent

147

## Description

Replace ERP demo RoleIds with product roles (Member, Operator, Administrator, Developer)
and centralize role→policy grants for SPA authorization.

## Checklist

- [x] RoleIds product set only (stable Administrator/Developer Guids kept)
- [x] RolePolicyGrants SSOT + PolicyRegistration uses it
- [x] RoleStore seed product roles
- [x] Identity-session principals get Member role claim
- [x] Mock / GetCurrentUser mocks updated
- [x] Tests green; build 0/0

## Session

- Orchestrate 147-002: 2026-08-04

## Results

### Summary

**Product RoleIds:** Member, Operator, Administrator, Developer (ERP roles removed).

**RolePolicyGrants** maps:
| Policy | Roles |
|--------|--------|
| Admin nav/page/roles | Administrator |
| Developer nav/page/claims | Developer |

**Identity-session:** every passkey principal gets `ClaimTypes.Role = Member` until principal-role store (147-004). No Developer/Admin without assignment/mock.

**Mock:** Member + Administrator + Developer (still sees demos).

### Files

- `role-ids-contracts.cs`, `role-store-application.cs`, `role-policy-grants.cs`
- `policy-registration.cs`, page/nav registration docs
- `identity-session-authentication-state-provider.cs`, mock auth, GetCurrentUser/GetRoles mocks
- serialization tests

### Build / tests

- `./bin/dev build` 0/0
- web-contracts-tests Role* 6/6
- web-server-integration-tests Roles* 8/8

### Review

clean, effort 1

### How to validate

**Automated**
```bash
./bin/dev build
# expect: 0/0
cd tests/container-apps/web/web-contracts-tests && dotnet test -c Release -- --filter-class Role
# expect: all passed
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Roles
# expect: all passed
```

**Manual (passkey, UseMock false)**
1. Sign in with passkey
2. **Expect:** Home + Settings only (Member) — no Demos/Developer/Admin
3. Inspect claims or code: principal has Member Guid role claim

**Manual (mock UseMock true)**
1. **Expect:** Demos + Developer + Admin nav (mock has Developer + Administrator)

**Not in scope:** principal-role persistence/assignment UI (147-004); marketplace Operator policies (118).
