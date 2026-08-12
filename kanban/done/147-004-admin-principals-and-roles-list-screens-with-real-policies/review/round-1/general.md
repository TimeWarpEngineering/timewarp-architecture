# Round 1 — general
**Date:** 2026-08-04
**Scope reviewed:** commit a0007945 vs a0b22bb4 (147-004)

## Summary

The 147-004 surface is largely aligned with the locked decisions: web-app `IPrincipalRoleStore` + `EffectiveRolesResolver` (empty → Member; non-empty exact; bootstrap unions Admin+Member), cookie stays PrincipalId-only with `PrincipalRoleClaimsTransformation`, admin contracts use `CanViewRolesPage` / `CanViewPrincipalsPage` with server `RequireRole(Administrator)`, and GetCurrentSession `RoleIds` feeds SPA `ClaimTypes.Role`. Authz HTTP tests correctly prove Member 403 → store-granted Admin 200 and scheme isolation for agent bearer. The main product gap is admin UI edit semantics: list drafts are seeded from *effective* roles but post-save state is updated from *stored* roles, so virtual grants (default Member and bootstrap Admin) can desync from what RequireRole still sees until refresh.

## Issues

### Issue 1 — Severity: bug
- File: `source/container-apps/web/projects/web-spa/features/admin/principals/principal-state/principal-state.set-principal-roles.cs:81-98`
- Description: `ListPrincipals` returns *effective* roles (resolver: empty store → Member; bootstrap unions Admin+Member) and `FetchPrincipals` seeds drafts from those rows. After `SetPrincipalRoles`, `HandleSuccess` overwrites draft + row `RoleIds` from the command response, which intentionally echoes *stored* roles only (`set-principal-roles-handler-application.cs` Design). For any principal whose effective set includes virtual grants, an admin can uncheck those boxes and Save: the store write succeeds, the UI shows the demotion, but the next request’s claims transform / resolver still applies the virtual grants. Reload re-checks the boxes. Affects both empty→Member (uncheck all) and bootstrap Admin (uncheck Administrator). Handler Design expects List to re-read effective roles; the SPA never re-fetches after save.
- Suggestion: After successful Set, either re-run `FetchPrincipals` (or re-resolve effective roles for that row), or change the API so Response carries effective RoleIds (and keep a separate StoredRoleIds if the UI needs both). Prefer making the multi-select bind to *stored* assignment and display effective grants as read-only badges if virtual roles must stay non-editable.
- Status: open

### Issue 2 — Severity: suggestion
- File: `source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs:33-50`
- Description: The SSOT algorithm is clear and matches the plan, but there is no focused unit/Jaribu coverage of empty → Member, non-empty exact (including Administrator-only), bootstrap union, invalid bootstrap Guid ignore, and `RoleIds.All` ordering. Existing authz suites only exercise Member-only 403 and store-granted Admin 200.
- Suggestion: Add a small host-free test class against `EffectiveRolesResolver` + in-memory store/options (and optionally one HTTP test that bootstrap config alone yields 200 on an admin route without a store write).
- Status: open

### Issue 3 — Severity: suggestion
- File: `source/container-apps/web/features/admin/principals/set-principal-roles/set-principal-roles-handler-application.cs:48-51`
- Description: Any Administrator can clear or demote any principal, including themselves and the last Administrator. Empty assignment is allowed (D10) and correctly becomes effective Member. There is no last-admin guard and no SPA warning; recovery depends on `Authentication:BootstrapAdministratorPrincipalIds` (or a surviving store assignment). Integration test `Ok_SetRoles_Given_Administrator_Via_Role_Store` deliberately demotes the session principal to Member+Developer, which proves the hole is exercised.
- Suggestion: Document self/last-admin lockout + bootstrap recovery in the Principals page copy (and task notes). Optional follow-up: reject clearing Administrator from the last admin principal with a problem details response.
- Status: open

### Issue 4 — Severity: suggestion
- File: `source/container-apps/web/projects/web-spa/features/admin/principals/principal-state/principal-state.set-principal-roles.cs:81-102`
- Description: Changing the signed-in principal’s roles does not call `IdentitySessionAuthenticationStateProvider.NotifySessionChanged()`. Server claims re-resolve per request via `IClaimsTransformation`, but SPA `AuthorizeView` / nav keep the session snapshot from last GetCurrentSession until remount or re-login. Self-demotion leaves Admin chrome visible while APIs start returning 403; self-promotion of another device session is the inverse.
- Suggestion: On successful Set for the current principal id, notify session changed (or always re-fetch session after any SetPrincipalRoles).
- Status: open

### Issue 5 — Severity: suggestion
- File: `source/container-apps/web/projects/web-server/appsettings.json:20-24`
- Description: `BootstrapAdministratorPrincipalIds` is bound from the shared `Authentication` section in all environments (not Development-only). Default is empty (safe). Any deployment that can inject this array grants permanent effective Administrator+Member without a store write; the Lazy parse in `EffectiveRolesResolver` also freezes the set for process lifetime (restart required after config change).
- Suggestion: Keep empty default; document that bootstrap is a break-glass operator control with host restart, and consider binding/enforcing only when `IHostEnvironment.IsDevelopment()` (or an explicit `Authentication:EnableBootstrapAdministrators` flag) if production config surfaces are less trusted than intended.
- Status: open

### Issue 6 — Severity: nit
- File: `source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:53-56`
- Description: `FluentCheckbox` `CheckedChanged` ignores the new `bool?` and always toggles draft membership. Correct when the event fires once against a draft that still matches `Checked`, but brittle if the component re-enters or the draft was out of sync.
- Suggestion: Set membership from `value == true` (add/remove explicitly) instead of toggle.
- Status: open
