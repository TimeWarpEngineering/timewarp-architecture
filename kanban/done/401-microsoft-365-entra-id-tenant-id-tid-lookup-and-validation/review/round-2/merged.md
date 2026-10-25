# Round 2 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 1 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:112
- Description: Host `AddStandardResilienceHandler` turned a Microsoft 503 into `Timeout`, and
  Polly exceptions escaped the typed results.
- Source: general (round 1)
- Disposition notes: Fixed in `81ad53928`. `Register` removes `ResilienceHandler`, the catch maps
  all non-cancellation exceptions, and a test proves 503 → `UnexpectedStatus` after one call
  under the host's handler order.

### M2 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/settings/entra-tenant-ids.cs:16
- Description: The public `ConvertLegacyGuidArrayJson` was a second, untied conversion.
- Source: general (round 1)
- Disposition notes: Fixed in `81ad53928`. The method and its test are deleted, and the Design
  region names the migration and its EF test.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:67
- Description: A second domain for a listed tenant produced a confusing "more than one domain"
  message.
- Source: general (round 1)
- Disposition notes: Fixed in `81ad53928`, with the "already listed under {domain}" message. The
  fix goes too far for null-domain rows. See M6.

### M4 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:137
- Description: Cache entries had no `Size`.
- Source: general (round 1)
- Disposition notes: Fixed in `81ad53928`. `Size = 1`, with a `SizeLimit` test.

### M5 — Severity: nit — Status: fixed
- File: kanban/in-progress/401-microsoft-365-entra-id-tenant-id-tid-lookup-and-validation/task.md
- Description: How to validate gave the test override as the 429 threshold.
- Source: general (round 1)
- Disposition notes: Fixed in `81ad53928`.

### M6 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:67
- Description: The M3 early return also catches an existing entry whose domain is null. Before,
  `TryNormalize` filled in the domain on lookup. Now a migrated or hand-entered GUID row cannot
  get its domain from a lookup. The page says "already listed" and the row stays a bare GUID. The
  test `Leave_A_Guid_Only_Row_When_Lookup_Finds_That_Tenant` pins this regression.
- Suggestion: return early only when `entry.Domain is not null`. Otherwise let `TryNormalize`
  replace the row with `{id, domain}`. Flip the GUID-only test to expect the domain, and update the
  Design region.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
