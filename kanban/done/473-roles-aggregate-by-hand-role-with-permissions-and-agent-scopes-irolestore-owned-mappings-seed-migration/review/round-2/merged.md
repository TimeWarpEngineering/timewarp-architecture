# Round 2 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/admin/roles/roles-endpoint-tests.cs:194
- Description: (carried from round 1) No test sent `DeleteRole` for a product role and asserted 409.
- Suggestion: add an endpoint test for every `RoleIds.All` id.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. `Conflict_Given_Product_Role` asserts 409 and then
  asserts that `GetRole` still resolves the role, for each product role. The integration suite
  passes (436 passed, 1 skipped, 0 failed), re-run by the reviewer.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
