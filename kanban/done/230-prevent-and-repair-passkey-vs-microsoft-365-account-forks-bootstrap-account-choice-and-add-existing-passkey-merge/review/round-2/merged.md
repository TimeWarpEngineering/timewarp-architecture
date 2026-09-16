# Round 2 — merged findings
**Date:** 2026-09-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:139
- Description: Link of a revoked foreign Entra handle merged the owner instead of 403.
- Suggestion: Refuse revoked rows with EntraCredentialRevoked; merge only when `existing is { IsRevoked: false }`.
- Source: general (round 1)
- Disposition notes: Confirmed in round 2. ProcessLinkAsync 403s revoked handles; regression `Link_Foreign_Revoked_Handle_Should_Refuse_Without_Merge`.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:76
- Description: Create-new-account did not NotifySessionChanged after mint.
- Suggestion: Notify IdentitySessionAuthenticationStateProvider before NavigateTo.
- Source: general (round 1)
- Disposition notes: Confirmed in round 2. Create path notifies then NavigateTo.

### M3 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs:237
- Description: UpdateCredentialAsync allowed PrincipalId re-parent outside MergePrincipalAsync.
- Suggestion: Reject PrincipalId changes in both store Update implementations.
- Source: general (round 1)
- Disposition notes: Confirmed in round 2 for the store guards. Coverage wiring is M5.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:392
- Description: OnValidatePrincipal rejected without signing out the cookie.
- Suggestion: SignOutAsync after RejectPrincipal on both paths.
- Source: general (round 1)
- Disposition notes: Confirmed in round 2.

### M5 — Severity: bug — Status: open
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-contract-tests.cs:82; tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs:146
- Description: Shared contract method `Update_rejects_PrincipalId_reparent` was added, but neither InMemory nor EF Jaribu runner exposes a static wrapper, so the regression never executes.
- Suggestion: Add the same static forwarding wrappers used by sibling Credentials tests in both runners, next to `Update_missing_credential_fails`.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- M3 product fix is confirmed; M5 is the missing test harness wiring for that same regression, not a re-open of M3.
