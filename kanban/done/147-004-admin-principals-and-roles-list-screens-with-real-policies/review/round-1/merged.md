# Round 1 — merged findings
**Date:** 2026-08-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: principal-state.set-principal-roles.cs HandleSuccess
- Description: After Set, drafts patched from *stored* roles while list seeds *effective* roles → virtual Member/bootstrap Admin desync
- Suggestion: Re-fetch ListPrincipals after success
- Source: general
- Disposition notes: HandleSuccess now `MediatorSender.Send(FetchPrincipalsActionSet.Action)` so drafts re-seed from effective RoleIds

### M2 — Severity: suggestion — Status: fixed
- File: effective-roles-resolver-application.cs
- Description: No unit coverage of SSOT algorithm
- Source: general
- Disposition notes: Added co-located Jaribu `effective-roles-resolver-tests.cs` (6 cases: empty, exact, bootstrap union, bootstrap empty, invalid Guid, ordering)

### M3 — Severity: suggestion — Status: fixed
- File: set-principal-roles handler / PrincipalsPage
- Description: Self/last-admin demotion allowed; document recovery
- Source: general
- Disposition notes: PrincipalsPage subtitle documents bootstrap recovery and lockout risk. Last-admin guard deferred (template accepts bootstrap break-glass)

### M4 — Severity: suggestion — Status: fixed
- File: principal-state.set-principal-roles.cs
- Description: No NotifySessionChanged after editing signed-in principal
- Source: general
- Disposition notes: After re-fetch, if edited PrincipalId matches identity-session NameIdentifier / timewarp:principal_id, call NotifySessionChanged()

### M5 — Severity: suggestion — Status: wontfix
- File: appsettings.json BootstrapAdministratorPrincipalIds
- Description: Bootstrap bound in all environments (empty default safe); consider Development-only
- Source: general
- Disposition notes: Plan locked bootstrap as break-glass available always with empty default. Restricting to Development would block Production recovery. Documented on Principals page. Decided by: orchestrator

### M6 — Severity: nit — Status: fixed
- File: PrincipalsPage.razor checkbox
- Description: Toggle ignores CheckedChanged bool?
- Source: general
- Disposition notes: SetRoleSelectedActionSet sets membership from `value == true`

## Duplicates / conflicts

- None
