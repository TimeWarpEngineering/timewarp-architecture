# Enforce page policies on all product and admin routes

## Parent

147

## Description

Every product SPA page declares a named policy on `[Page]` and `[Authorize]` so nav and
direct URLs agree. Demos/admin already gated in 147-001; this task covers self-service
and remaining product routes.

## Checklist

- [x] Policies.Authenticated (RequireAuthenticatedUser)
- [x] CanViewOwnProfile / CanViewSettings in constants + RolePolicyGrants (all product roles)
- [x] Profile, Settings, ChangePassword gated
- [x] Home, Login, Logout remain public (documented)
- [x] Demo/admin pages still Developer/Administrator from 147-001
- [x] Build 0/0

## Session

- 2026-08-04 orchestrate 147-003

## Results

### Summary

| Page | Policy |
|------|--------|
| `/`, `/Login`, `/Logout` | Anonymous (default) |
| `/Profile` | CanViewOwnProfile (Member+) |
| `/Settings` | CanViewSettings (Member+) |
| `/changePassword` | Authenticated |
| Admin / Demos / Developer | unchanged from 147-001 |

Self-service grants: Member, Operator, Administrator, Developer (all product roles).

### Build / tests

- `dotnet build web-spa` / `./bin/dev build` expected 0/0

### Review

clean, effort 1

### How to validate

**Automated**
```bash
dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release
# expect: 0/0
rg -n '\[Page\(|\[Authorize' source/container-apps/web/projects/web-spa --glob '*Page*.cs'
# expect: Profile/Settings/ChangePassword have Policy + Authorize; demos have Developer; admin Roles has CanViewRolesPage
```

**Manual (passkey Member)**
1. Signed out: open `/Profile` → redirect Login
2. Signed in: `/Profile` and `/Settings` work; Settings in nav
3. Signed out: Settings not in nav (CanViewSettings fails)

**Manual (no Developer)**
- `/Counter` still blocked (147-001)

**Not in scope:** principal-role assignment UI (147-004); home chrome redesign (147-005).
