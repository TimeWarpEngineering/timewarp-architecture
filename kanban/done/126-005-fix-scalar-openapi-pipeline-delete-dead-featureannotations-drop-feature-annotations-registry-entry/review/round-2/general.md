# Round 2 — general
**Date:** 2026-07-26
**Scope reviewed:** fix commit 5e6ba287 (base implement 429d5d65); re-verify M1–M4 post-fix

## Summary

Re-verified all four round-1 findings against the post-fix tree. **M1–M4 are fixed.** Design/docs now state the FE filter vs OpenAPI `WithTags` split correctly; reference sample pairs both; durable Aspire-hosted `GET /openapi/v1.json` coverage exists with a `WeatherForecasts` tag assertion; api-server no longer carries a direct Scalar using or PackageReference. Fix delta (`AllowEmptyRequestDtos=true` on both hosts, OpenApiDocument test, region/doc rewrites, Scalar cleanup) introduces no new defects. Zero open issues.

## Prior findings re-check

### M1 — common-server-module Design / WithTags — **fixed**
- File: `source/foundation/foundation-server/common-server-module.cs:14-21`, `:78`, `:86-88`
- Design region now states OpenAPI/Scalar tags come from generator `Description.WithTags` (namespace leaf under Features + additive `[OpenApiTags]`); FE `Tags()` is filter-only; `AutoTagPathSegmentIndex=0` disables competing route-segment auto-tags; `OpenApiDocument` (not raw `AddOpenApi`); `MapOpenApi` after `UseFastEndpoints`.
- Inline comments at `AddOpenApi` match the same model. No residual “Tags() drives Scalar sidebar” claim.

### M2 — ApiEndpointSourceGenerator.md sample — **fixed**
- File: `documentation/developer/reference/ApiEndpointSourceGenerator.md:78-114`, OpenAPI section ~147–156
- Sample `Configure()` pairs `Tags("WeatherForecasts")` with `Description(d => d.WithTags("WeatherForecasts").Produces…)`.
- `AllowAnonymous()` appears only as emission from `[EndpointAllowAnonymous]` (explicit fail-closed note; not a default).
- Namespace-leaf tag source + additive `[OpenApiTags]` documented; folder paths do not set tags.

### M3 — OpenApiDocument host test + template-smoke — **fixed**
- File: `tests/container-apps/api/api-server-integration-tests/Features/OpenApi/OpenApiDocument_Tests.cs`
- Exists and is sensible: Aspire-launched api-server (avoids in-process AppDomain pollution from web types via timewarp-testing), asserts HTTP 200 on `/openapi/v1.json` and at least one operation tag `WeatherForecasts` (generator leaf …Features.WeatherForecasts).
- Round-1 disposition: suite 7 passed / 1 skipped; `dev template-smoke` both matrices (SmokeDefault + SmokeNoPostgres) green after clearing stale global NuGet `2.0.0-smoke` cache. Web-server standalone curl still intentionally skipped (Passwordless ApiSecret bare-host constraint) — residual of record, not a regression.

### M4 — api-server Scalar using / PackageReference — **fixed**
- `source/container-apps/api/api-server/global-usings.cs`: no `Scalar.AspNetCore` global using.
- `source/container-apps/api/api-server/api-server.csproj`: no direct `Scalar.AspNetCore` PackageReference; types come through foundation-server (`ProjectReference` / `TimeWarp.Foundation.Server`).

## New issues from fix delta

None.

## Issues

_(none)_
