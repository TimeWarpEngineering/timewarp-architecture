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
- Implemented 2026-08-04: Phases A–E complete. Role store/resolver live in Features
  substrate namespace (TWA0009: Identity GetCurrentSession + Admin both need them).
  Build 0/0; roles-authorization 6/6; principals-authorization 5/5; List_principals
  contract 2/2; Jaribu list/set principal contracts green.

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
