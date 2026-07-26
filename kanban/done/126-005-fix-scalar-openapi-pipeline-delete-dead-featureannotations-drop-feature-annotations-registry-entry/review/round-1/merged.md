# Round 1 — merged findings
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
- Disposition notes: Rewrote Design region and the `OpenApiDocument` inline comment. Now states OpenAPI/Scalar tags come from generator `Description.WithTags` (namespace leaf under Features + additive `[OpenApiTags]`); FE `Tags()` is filter-only; `AutoTagPathSegmentIndex=0` disables competing route-segment auto-tags; `OpenApiDocument` (not raw `AddOpenApi`); `MapOpenApi` after `UseFastEndpoints`.

### M2 — Severity: suggestion — Status: fixed
- File: `documentation/developer/reference/ApiEndpointSourceGenerator.md:79-97` (OpenAPI ~138–144)
- Description: Reference doc still shows `Tags` without `.WithTags(...)` and stale `AllowAnonymous()` default sample.
- Suggestion: Update sample to pair Tags + WithTags; drop fail-open AllowAnonymous; note namespace leaf under Features sets tags.
- Source: general
- Disposition notes: Sample `Configure()` now pairs `Tags(...)` with `Description(d => d.WithTags(...).Produces...)`. Notes that `AllowAnonymous()` is only emitted from `[EndpointAllowAnonymous]` (not a fail-open default). OpenAPI section updated: tag source is namespace leaf under Features (folder paths do not set tags); `[OpenApiTags]` additive; both FE filter tags and OpenAPI WithTags are emitted.

### M3 — Severity: suggestion — Status: fixed
- File: `tests/container-apps/api/api-server-integration-tests/` (missing coverage)
- Description: No durable host test for `GET /openapi/v1.json` 200 + operation tags; template-smoke both matrices not run; web-server OpenAPI not curl-proven standalone.
- Suggestion: Thin api-server integration assertion; run `dev template-smoke`; optional web under Aspire.
- Source: general
- Disposition notes: Added `Features/OpenApi/OpenApiDocument_Tests.cs` — Aspire-launched api-server `GET /openapi/v1.json` returns 200 and at least one operation tag is `WeatherForecasts` (uses DistributedApplication, not in-process ApiTestServerApplication, to avoid AppDomain pollution from web-server types loaded via timewarp-testing). Suite: 7 passed, 1 skipped. `dev template-smoke` both matrices (SmokeDefault + SmokeNoPostgres) succeeded after clearing a stale global NuGet `2.0.0-smoke` cache that shadowed the smoke-local feed with an older Foundation.Server API (typeArray). Web-server standalone curl still skipped (Passwordless ApiSecret bare-host constraint). Also set `AllowEmptyRequestDtos=true` on both hosts so FE.OpenApi accepts propertyless request DTOs (web identity/profile empty Queries) — without it `/openapi/v1.json` throws on those endpoints.

### M4 — Severity: nit — Status: fixed
- File: `source/container-apps/api/api-server/global-usings.cs:11`; `api-server.csproj:19`
- Description: Unused `global using Scalar.AspNetCore` and possibly redundant PackageReference after Scalar moved to CommonServerModule.
- Suggestion: Drop unused using; PackageReference only if dual-mode packaging requires it.
- Source: general
- Disposition notes: Dropped `global using Scalar.AspNetCore` and direct `Scalar.AspNetCore` PackageReference from api-server. Types come through foundation-server (ProjectReference or TimeWarp.Foundation.Server package). Build 0/0 without the direct ref.

## Duplicates / conflicts

- None
