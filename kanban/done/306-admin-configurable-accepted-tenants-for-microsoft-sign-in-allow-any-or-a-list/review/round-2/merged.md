# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-tests.cs:168
- Description: The challenge fixture seeded settings without accepted tenants, so 10 tests failed under the new Restricted default.
- Source: general (round 1)
- Disposition notes: e00a0e7dd seeds `[TrustedTenantId]` and adds an Any-mode bootstrap case. web-server-integration-tests: 349 passed, 0 failed, 1 skipped.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/settings/site-settings-details-contracts.cs:45
- Description: The validator restated the tenant-list rules instead of calling `EntraTenantIds.TryNormalize`.
- Source: general (round 1)
- Disposition notes: e00a0e7dd validates through `TryNormalize` and updates the Design region. Two new tests cover the raw vs distinct cap. Runfile 30/30.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:188
- Description: The help text named only `common` for personal accounts.
- Source: general (round 1)
- Disposition notes: e00a0e7dd says "common (or consumers)".

## New findings

None on the fix delta.

## Duplicates / conflicts

- None (single reviewer).
