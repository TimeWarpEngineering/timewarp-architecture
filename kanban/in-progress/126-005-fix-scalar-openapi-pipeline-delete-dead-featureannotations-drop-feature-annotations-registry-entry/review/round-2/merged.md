# Round 2 — merged findings
**Date:** 2026-07-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `source/foundation/foundation-server/common-server-module.cs:14-17` and `:76`
- Description: Design region and inline comment still claim generator-emitted `Tags()` drive Scalar’s feature-grouped sidebar. FE `Tags()` is filter-only; OpenAPI/Scalar needs `Description.WithTags`.
- Suggestion: Rewrite to say OpenAPI tags come from generator `Description.WithTags` (leaf Features / `[OpenApiTags]`); `Tags()` is filter-only; `AutoTagPathSegmentIndex=0` disables route-segment auto-tags.
- Source: general
- Disposition notes: (r1) Rewrote Design region and the `OpenApiDocument` inline comment. (r2 re-verify) Design lines 14–21 and comments at 78 / 86–88 correctly document `Description.WithTags` as OpenAPI/Scalar source, FE `Tags()` as filter-only, and `AutoTagPathSegmentIndex=0`. Confirmed fixed.

### M2 — Severity: suggestion — Status: fixed
- File: `documentation/developer/reference/ApiEndpointSourceGenerator.md:79-97` (OpenAPI ~138–144)
- Description: Reference doc still shows `Tags` without `.WithTags(...)` and stale `AllowAnonymous()` default sample.
- Suggestion: Update sample to pair Tags + WithTags; drop fail-open AllowAnonymous; note namespace leaf under Features sets tags.
- Source: general
- Disposition notes: (r1) Sample pairs `Tags` + `Description.WithTags`; auth from posture markers only; OpenAPI section updated. (r2 re-verify) Sample at 86–102 and OpenAPI bullets 147–156 match production emission. Confirmed fixed.

### M3 — Severity: suggestion — Status: fixed
- File: `tests/container-apps/api/api-server-integration-tests/` (missing coverage)
- Description: No durable host test for `GET /openapi/v1.json` 200 + operation tags; template-smoke both matrices not run; web-server OpenAPI not curl-proven standalone.
- Suggestion: Thin api-server integration assertion; run `dev template-smoke`; optional web under Aspire.
- Source: general
- Disposition notes: (r1) Added `Features/OpenApi/OpenApiDocument_Tests.cs` (Aspire-hosted, 200 + `WeatherForecasts` tag); suite green; `dev template-smoke` both matrices green; web bare-host curl skipped intentionally; `AllowEmptyRequestDtos=true` on both hosts for FE.OpenApi empty DTOs. (r2 re-verify) Test file present and correctly scoped; disposition accepted. Confirmed fixed.

### M4 — Severity: nit — Status: fixed
- File: `source/container-apps/api/api-server/global-usings.cs:11`; `api-server.csproj:19`
- Description: Unused `global using Scalar.AspNetCore` and possibly redundant PackageReference after Scalar moved to CommonServerModule.
- Suggestion: Drop unused using; PackageReference only if dual-mode packaging requires it.
- Source: general
- Disposition notes: (r1) Dropped global using and direct PackageReference; types via foundation-server. (r2 re-verify) Neither present in api-server usings/csproj. Confirmed fixed.

## New issues (round 2)

None.

## Duplicates / conflicts

- None
