# Round 1 — merged findings
**Date:** 2026-08-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-infrastructure-tests/ef-principal-role-store-tests.cs
- Description: EF `TryClaimFirstAdministratorAsync` had no first-wins coverage.
- Suggestion: Sequential first/second claim on two contexts, one database.
- Source: general
- Disposition notes: Added `TryClaimFirstAdministrator_first_wins_second_stays_unassigned`.

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs:126
- Description: No handler-level fail-registration / sign-in-no-claim tests.
- Suggestion: In-proc cases for 409/ceremony fail and authenticate-stays-Member.
- Source: general
- Disposition notes: Sign-in and agent paths do not inject `IPrincipalRoleStore` (grep-confirmed; claim-on-sign-in reverted 1819a600). Failed registration returns before claim. Extra host tests would be order-sensitive on shared fixtures. Orchestrator: wontfix.

## Duplicates / conflicts

- None.
