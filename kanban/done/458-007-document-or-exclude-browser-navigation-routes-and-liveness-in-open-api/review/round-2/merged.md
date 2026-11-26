# Round 2 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:80
- Description: `GET /alive` (`HealthEndpointPaths.Alive`) is on the shared exclusion list. `Default_Endpoints_Should_Be_Listed` maps `MapDefaultEndpoints` on a Production host and checks those routes against the real web-server document. `MapDefaultEndpoints` serves that path in every environment. The ingress rewrite and catch-all match the entry's reason.
- Suggestion: none
- Source: general
- Disposition notes: Re-verified in round 2 against commit `fd3552530`. Fixed in the round 1 fix pass.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:54
- Description: `info.description` includes `OpenApiDiscoveryCoverage.UnlistedFamilies`, and `Unlisted_Families_Should_Name_What_The_Route_Walk_Skips` pins those phrases. The route walk skips that same set.
- Suggestion: none
- Source: general
- Disposition notes: Re-verified in round 2. Fixed in the round 1 fix pass.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/web-server-open-api-excluded-routes.cs:43
- Description: The dictation path is listed for `GET` and `CONNECT`. The walk compares method and path when the endpoint declares methods. `Dictation_Should_List_Every_Mapped_Method` requires both. The moved test did not weaken an assertion.
- Suggestion: none
- Source: general
- Disposition notes: Re-verified in round 2. Fixed in the round 1 fix pass.

## Duplicates / conflicts

- One reviewer. Prior IDs carried forward. No new findings.
