# Round 3 — merged findings
**Date:** 2026-09-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:139
- Description: Link of a revoked foreign Entra handle merged the owner instead of 403.
- Suggestion: Refuse revoked rows with EntraCredentialRevoked; merge only when `existing is { IsRevoked: false }`.
- Source: general (round 1)
- Disposition notes: Carried fixed from round 2.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:76
- Description: Create-new-account did not NotifySessionChanged after mint.
- Suggestion: Notify IdentitySessionAuthenticationStateProvider before NavigateTo.
- Source: general (round 1)
- Disposition notes: Carried fixed from round 2.

### M3 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs:237
- Description: UpdateCredentialAsync allowed PrincipalId re-parent outside MergePrincipalAsync.
- Suggestion: Reject PrincipalId changes in both store Update implementations.
- Source: general (round 1)
- Disposition notes: Carried fixed from round 2.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:392
- Description: OnValidatePrincipal rejected without signing out the cookie.
- Suggestion: SignOutAsync after RejectPrincipal on both paths.
- Source: general (round 1)
- Disposition notes: Carried fixed from round 2.

### M5 — Severity: bug — Status: fixed
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-contract-tests.cs:85; tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs:147
- Description: Shared contract method `Update_rejects_PrincipalId_reparent` had no Jaribu static wrappers, so it never ran.
- Suggestion: Add forwarding wrappers in both InMemory and EF Credentials runners.
- Source: general (round 2)
- Disposition notes: Both runners wrap the method. Credentials filter 13/13 in identity-tests and web-infrastructure-tests.

## Duplicates / conflicts

- None. No new findings in round 3.
