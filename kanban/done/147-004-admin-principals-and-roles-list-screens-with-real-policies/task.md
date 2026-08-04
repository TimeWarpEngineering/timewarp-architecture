# Admin principals and roles list screens with real policies

## Parent

147

## Description

Deliver an honest admin surface for the passkey identity path:

1. **Principal → role assignment** (web-app store + effective-role resolution).
2. **Session roles** flow into SPA claims so existing SPA policies work for real
   passkey users (not only mock).
3. **Server admin APIs** require **Administrator**, not any signed-in user.
4. **SPA**: Roles list + Principals list (with role assignment) under Admin nav.
5. **Bootstrap first Administrator** via Development config (no auto-promote first
   registrant).

Today: passkey principals are hardcoded Member; Role CRUD is any-authenticated;
SPA only has “New Role” form; no principals admin UI.

## Checklist

- [x] AuthorizationPolicyNames substrate + SPA CanViewPrincipalsPage + RolePolicyGrants
- [x] IPrincipalRoleStore + InMemory + IEffectiveRolesResolver + bootstrap options
- [x] IPrincipalStore.ListPrincipalsAsync (port + InMemory + Ef + contract tests)
- [x] IClaimsTransformation for role claims on server
- [x] Server policies CanViewRolesPage / CanViewPrincipalsPage (RequireRole Administrator)
- [x] GetCurrentSession.Response.RoleIds + SPA IdentitySessionAuthenticationStateProvider
- [x] Tighten five role contracts to admin policy; update roles-authorization-tests
- [x] ListPrincipals + SetPrincipalRoles contracts/handlers/tests
- [x] SPA RolesListPage + PrincipalsPage + nav; RoleForm navigates to list
- [x] Build 0/0; targeted tests green; Design regions reconciled

## Session

- Orchestrate 147-004: 2026-08-04
- Plan: 019fcbd2-8712-73f2-bb04-fa343d3534ca
- Implemented: 019fcbd6-87a3-7aa0-b912-91a7a6df1660
- Review round-1: 019fcbe6-354d-7dd2-88ee-a3b12f1965d7 → disposition accepted-exceptions

## Results

### Summary

Passkey principals get **effective roles** (empty store → Member; bootstrap can union
Administrator). Admin APIs require **Administrator**. SPA Admin nav: **Roles** list +
**Principals** with inline multi-select role assignment.

| Surface | Behavior |
|---------|----------|
| `IPrincipalRoleStore` + `EffectiveRolesResolver` | Features substrate (TWA0009-safe) |
| `GetCurrentSession.RoleIds` | Feeds SPA `ClaimTypes.Role` |
| `IClaimsTransformation` | Server RequireRole on admin endpoints |
| `ListPrincipals` / `SetPrincipalRoles` | Admin-only APIs |
| Bootstrap | `Authentication:BootstrapAdministratorPrincipalIds` |

### Commits

- `a0007945` feat(web): admin principals and roles with real policies
- `b9612122` fix(web): re-fetch effective roles after principal role save

### Build / tests

| Gate | Result |
|------|--------|
| `./bin/dev build` | 0/0 |
| RolesAuthorization | 6/6 |
| PrincipalsAuthorization | 5/5 |
| EffectiveRolesResolver Jaribu | 6/6 |
| List/set principal contracts Jaribu | green |

### Review

- Effort 1, general, 1 round
- Disposition: **accepted-exceptions** (M5 bootstrap all-env empty default wontfix)
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`
- Fixed: M1 re-fetch after Set; M2 resolver tests; M3 page copy; M4 NotifySessionChanged; M6 checkbox

### How to validate

**Automated**
```bash
./bin/dev build
# expect: 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests && \
  dotnet test -c Release -- --filter-class RolesAuthorization
# expect: all passed (Member 403, Admin 200)

cd tests/container-apps/web/web-server-integration-tests && \
  dotnet test -c Release -- --filter-class PrincipalsAuthorization
# expect: all passed

dotnet run source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs
# expect: 6 passed
```

**Manual (passkey, UseMock false)**
1. Sign in with passkey → **Expect:** no Admin nav (Member only).
2. Note `principalId` from `GET api/identity/session` (network tab).
3. Set `Authentication:BootstrapAdministratorPrincipalIds: ["<guid>"]` in web-server
   appsettings.Development.json; restart web-server.
4. Refresh SPA → **Expect:** Admin → Roles (4 product roles) + Principals (self).
5. On Principals, assign Developer (+ keep Administrator) → Save → **Expect:** demos appear
   after auth refresh (or hard reload).
6. Second passkey user remains Member-only until an admin assigns roles.

**Manual (mock UseMock true)**
- **Expect:** Admin + Roles/Principals usable offline (mock has Administrator).

**Not in scope:** EF principal-role tables; Operator marketplace policies (118);
home/login chrome (147-005).

## Notes

### Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Principal→role store | Web app `IPrincipalRoleStore` + in-memory (not TimeWarp.Identity) |
| D2 | Effective roles | Empty store → `{Member}`; non-empty → exactly those roles |
| D3 | Bootstrap | `Authentication:BootstrapAdministratorPrincipalIds` string[]; match → union Administrator+Member |
| D4 | Session claims | `GetCurrentSession.RoleIds` → SPA `ClaimTypes.Role` per Guid |
| D5 | GetCurrentUser | Leave ClientOnly / mock |
| D6 | Server policies | Shared capability name strings; RequireRole(Administrator Guid) |
| D7 | List principals | Extend `IPrincipalStore.ListPrincipalsAsync` |
| D8 | Server role claims | `IClaimsTransformation` each request (cookie stays PrincipalId-only) |
| D9 | Principal UI | List + inline multi-select role assignment (no detail route) |
| D10 | Empty SetPrincipalRoles | Allowed → effective Member only |

### Effective-role algorithm

```
stored = IPrincipalRoleStore.GetRoleIds(principalId)
effective = stored.Count == 0 ? { Member } : HashSet(stored)
if BootstrapAdministratorPrincipalIds contains principalId:
  effective += Administrator, Member
return ordered by RoleIds.All
```

### Implementation order

**A — foundation:** policy names, role store, ListPrincipalsAsync, claims transform,
server policies, GetCurrentSession.RoleIds, SPA provider.

**B — role API tighten:** five contracts → CanViewRolesPage; roles-authorization-tests
(Member 403, Admin 200).

**C — principals APIs:** ListPrincipals, SetPrincipalRoles + Jaribu + integration tests.

**D — SPA:** RolesListPage, PrincipalsPage, NavMenu, RoleForm navigate to list.

**E — docs/regions:** reconcile Design comments; full build/test.

### Out of scope

EF principal-role tables; Operator marketplace policies (118); home chrome (147-005);
Modules ERP cleanup; baking roles into session cookie.

### Pattern anchors

- Contracts: create-role / get-roles
- Authz tests: roles-authorization-tests.cs
- SPA list: WeatherForecastsPage
- Policies: role-policy-grants.cs
