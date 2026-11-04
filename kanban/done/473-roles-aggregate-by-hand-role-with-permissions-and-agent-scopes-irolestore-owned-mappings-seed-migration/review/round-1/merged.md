# Round 1 — merged findings
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
- File: source/container-apps/web/features/admin/roles/delete-role/delete-role-handler-application.cs:40
- Description: The acceptance criterion "`delete-role` returns 409 for product roles" has no test.
  The domain guard and the store refusals are tested, but nothing sends `DeleteRole` for a product
  role id. If the handler's `IsProductRole` branch were removed, the endpoint would answer 500 (the
  `InvalidOperationException` from `RemoveAsync`) and every test would still pass.
- Suggestion: Add an endpoint test in
  `tests/container-apps/web/web-server-integration-tests/features/admin/roles/roles-endpoint-tests.cs`.
  It sends `DeleteRole.Command` for a `RoleIds.All` member and asserts `IsT1` with
  `Status == 409`, then asserts that `GetRole` still resolves the role.
- Source: general
- Disposition notes: Fixed 2026-11-05 (implement-fix-2). Added
  `UpdateThenDelete_Roundtrip.Conflict_Given_Product_Role`, which covers every `RoleIds.All` id:
  it asserts 409, then asserts `GetRole` still resolves. A mutation check (guard disabled) fails it.

## Duplicates / conflicts

- None (single reviewer).
