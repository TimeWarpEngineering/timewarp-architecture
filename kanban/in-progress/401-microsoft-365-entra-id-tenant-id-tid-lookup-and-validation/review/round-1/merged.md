# Round 1 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:101
- Description: The web server's `AddServiceDefaults` adds `AddStandardResilienceHandler` to every
  HttpClient, including `entra-tenant-discovery`. In the host, a Microsoft 503 is retried and
  reported as `Timeout`. A probe gave `Outcome=Timeout`, 3 calls, 10.1 s, where the contract
  expects `UnexpectedStatus` 503. Polly's `TimeoutRejectedException` and `BrokenCircuitException`
  get past the typed catches and become an untyped 500. The Design region's "LookupTimeout is the
  only deadline" is false in the host.
- Suggestion: remove the default resilience handlers from the named client in `Register`, and/or
  map every exception except caller cancellation to `NetworkFailure`. Add a test with
  `ConfigureHttpClientDefaults(AddStandardResilienceHandler)` that asserts 503 gives
  `UnexpectedStatus` after one call. Fix the Design region.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/libraries/timewarp-identity/settings/entra-tenant-ids.cs:208
- Description: The public `ConvertLegacyGuidArrayJson` is called only by a unit test. The
  migration uses its own SQL. The Design region (line 16) wrongly says the migration test pins
  this method. That leaves two conversions with nothing tying them, and an old-shape converter in
  the published API.
- Suggestion: delete the method and its test. Point the Design region at the migration and
  `ef-entra-accepted-tenants-migration-tests.cs`.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:72
- Description: Looking up a second verified domain of a listed tenant shows "Tenant {guid} is
  listed with more than one domain." The list shows one row for that tenant, so the message
  confuses the admin.
- Suggestion: when the draft already has the tenant id, report that it is already listed under
  its stored domain, and leave the list as it is.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:129
- Description: `Cache.Set` on the shared `IMemoryCache` has no entry `Size`. It throws if a
  consuming app sets `SizeLimit`.
- Suggestion: set `Size = 1` on the entry options.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: kanban/in-progress/401-microsoft-365-entra-id-tenant-id-tid-lookup-and-validation/task.md:150
- Description: "How to validate" says the third anonymous GET is 429. That is true only with the
  test override. The default limit is 20 per 60 seconds.
- Suggestion: state the default threshold or name the override.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
