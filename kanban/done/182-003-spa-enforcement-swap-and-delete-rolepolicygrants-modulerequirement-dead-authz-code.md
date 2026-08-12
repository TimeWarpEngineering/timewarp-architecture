# SPA enforcement swap and delete RolePolicyGrants ModuleRequirement dead authz code

**Parent:** 182 · **Order:** C (after 182-002) · **Depends on:** 182-001, 182-002

## Description

SPA consumes the same permission registry/helper; session returns expanded permissions; delete dead module layer and RolePolicyGrants / dual constants.

## Requirements

- `GetCurrentSession` returns permissions from evaluator; auth-state provider projects them.
- SPA AddAuthorizationCore uses same registration helper as server.
- Delete: RolePolicyGrants, AuthorizationConstants.Policies (or fold), AuthorizationPolicyNames, ModuleRequirement*, ModuleIds, AuthorizationState.Modules, GetCurrentUser.Modules field, orphan CanViewAdminPage / CanViewUserClaims, inert page/nav registration placeholders as applicable.
- Mock SPA providers carry permission claims.
- **First SPA authz tests** (registry composition / AuthorizeView policy at minimum).

## Checklist

- [x] Session + auth-state provider
- [x] SPA policy registration from helper
- [x] Dead-code delete complete
- [x] SPA authz tests
- [x] Results + How to validate

## Session

- 2026-08-12: SPA enforcement swap — session Permissions, claim policies, delete RolePolicyGrants/modules, page/nav → PermissionIds.

## Results

### Summary

SPA authorization now uses the same **PermissionIds** vocabulary as the server:

- **GetCurrentSession.Response.Permissions** — expanded via `IPermissionEvaluator.GetPermissionsAsync` under identity-session scheme; unauthenticated → empty list.
- **IdentitySessionAuthenticationStateProvider** projects each permission as claim type `PermissionIds.ClaimType` (`"permission"`); still projects RoleIds for diagnostics.
- **PermissionPolicyRegistration.AddPermissionClaimPolicies** — SPA `RequireClaim("permission", id)` for every PermissionIds entry.
- **Server keeps AddPermissionPolicies** (PermissionRequirement → evaluator). SPA `PolicyRegistration` **skips** claim policies when a permission policy is already registered (web-server composes SPA ConfigureServices for prerender and must not overwrite server requirements).
- **Pages / NavMenu** use PermissionIds (AdminAccess, AdminRolesRead/Manage, AdminPrincipalsRead, DeveloperAccess, DeveloperClaimsRead, ProfileRead, SettingsRead).
- **MockAuthenticationStateProvider** projects `PermissionIds.All` as permission claims.
- **Deleted:** RolePolicyGrants, page/nav policy registration placeholders, ModuleRequirement*, ModuleIds, AuthorizationPolicyNames, AuthorizationState.Modules, GetCurrentUser.Modules; CanView* constants (kept only Anonymous/Authenticated).
- **GetCurrentUser** now returns Roles + Permissions (mock factories); Entra claims factory projects permission claims.
- **SPA authz tests:** `permission-claim-policies-tests.cs` (registry, claim success/fail, self-service composition, claim-type mismatch, server policies stay requirement-based).

### How to validate

```bash
./bin/dev build   # 0/0

dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs
dotnet run source/container-apps/web/features/authorization/permission-claim-policies-tests.cs

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class RolesAuthorization
dotnet test -c Release -- --filter-class PrincipalsAuthorization
```

Expect: build 0/0; evaluator 11/11; claim-policies 6/6; RolesAuthorization 7/7; PrincipalsAuthorization 6/6.
