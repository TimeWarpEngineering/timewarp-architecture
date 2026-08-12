# Gate demo and developer nav behind Developer policy

## Parent

147

## Description

Hide template demos and developer tooling from nav (and block direct routes) unless the
principal has the **Developer** role. Admin New Role requires **Administrator**.

## Checklist

- [x] Demo pages: `[Page(..., Policy = CanViewDeveloperPage)]` + `[Authorize(Policy=…)]`
- [x] UserClaims: CanViewUserClaimsPage
- [x] Admin Roles/New: CanViewRolesPage + Admin sidebar policy
- [x] NavMenu: Demos + Developer sections under CanViewDeveloperSidebarNavSection
- [x] NavMenu: Admin under CanViewAdminSidebarNavSection
- [x] Login removed from primary demo list (use profile Sign-in)
- [x] `./bin/dev build` 0/0 web-spa

## Results

### Summary

Demo and diagnostic pages now require **Developer** role for nav visibility and route access.
Admin Roles/New requires **Administrator**. Passkey-only users see Home + Settings (+ profile
menu), not Counter/Chat/Passkeys/StyleGuide/etc.

### How to validate

**Build**
```bash
dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release
# expect: 0/0
```

**Manual (passkey user without Developer role)**
1. `./bin/dev run` with UseMock false; passkey sign-in
2. **Expect nav:** Home, Settings, External — **no** Demos, Developer, Admin
3. Navigate to `/Counter` — **Expect:** NotAuthorized (Login or Forbidden)
4. Assign Developer role claim (or use mock with Developer) — **Expect:** Demos + Developer sections visible

**Manual (Administrator)**
- **Expect:** Admin section with New Role when principal has Administrator role claim

### Notes

Passkey identity-session does not yet assign roles (147-002/004). Until then only mock
(or future role assignment) surfaces Developer/Admin nav.

## Session

- Implemented: 2026-08-04
