# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-tests.cs:161
- Description: The fixture seeds site settings without `entraAcceptedTenantIds`. The new default (Restricted, empty list) refuses `TrustedTenantId`, so 10 challenge and callback tests fail in web-server-integration-tests (338 passed, 10 failed, the same on two runs).
- Suggestion: Seed `entraAcceptedTenantIds: [TrustedTenantId]`. Optionally add an Any-mode HTTP case for `UntrustedTenantId`. Run the full web-server-integration-tests assembly before Done.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/settings/site-settings-details-contracts.cs:41
- Description: The validator restates the tenant-list rules by hand instead of calling `EntraTenantIds.TryNormalize`, which the aggregate uses. The two already differ (raw count vs distinct count).
- Suggestion: Validate through `EntraTenantIds.TryNormalize`, as `AssistantNameRules.IsValid` does for the name. Update the Design region to match.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:187
- Description: The help text says personal accounts need authority `common`. The docs say `common` or `consumers`.
- Suggestion: Say "common (or consumers)".
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Out of scope (observed, not a finding on this diff)

- `get-entra-sign-in-offered-tests.cs` and `seed-on-read-site-settings-store-tests.cs` fail to build with NU1510 (explicit `Microsoft.Extensions.Options` / `Logging.Abstractions` package refs). Neither file is changed by this branch. The implementer already removed the same ref from `entra-sign-in-policy-tests.cs`, and the same boyscout fix would be welcome on these two.
