# Round 1 — general
**Date:** 2026-07-26
**Scope reviewed:** commit 429d5d65

## Summary

The OpenAPI + Scalar pipeline is wired correctly end-to-end on the shared path: `FastEndpoints.OpenApi` 8.2.0, `OpenApiDocument` with `DocumentName`/`AddDocument` both `"v1"`, `MapOpenApi` + `MapScalarApiReference` after `UseFastEndpoints` on web (always) and api (Development). Leaf feature tag derivation (`…Features.Admin.Roles` → `Roles`) and paired `Tags()` + `Description.WithTags(...)` emission match FastEndpoints’ documented split (filter vs OpenAPI tags), with generator tests covering WeatherForecast, nested Roles, and additive `[OpenApiTags]`. FeatureAnnotations files and the `feature-annotations` registry entry are gone from product code, JSON/`.g.cs`/`.g.props`/AGENTS.md/skill stay in sync; Microsoft.OpenApi **2.7.5** is the correct 2.x patch for GHSA-v5pm-xwqc-g5wc without jumping to 3.x (AspNetCore.OpenApi 10 incompatibility). Residual gaps are verification (template-smoke not run; web-server OpenAPI only proven via shared module + api curl) and a few doc/region accuracy nits below—no functional wiring bugs found.

## Issues

### Issue 1 — Severity: suggestion
- File: `source/foundation/foundation-server/common-server-module.cs:14-17` and `:76`
- Description: Design region and inline comment still claim generator-emitted **`Tags()`** (and “only explicit Tags()”) drive Scalar’s feature-grouped sidebar. This task’s own generator Design and runtime notes state FE `Tags()` is filter-only and OpenAPI/Scalar grouping requires `Description.WithTags`. The region was edited in this change but not fully reconciled, so a later maintainer can re-learn the wrong mental model (and might “fix” the pipeline by re-adding Tags-only emission).
- Suggestion: Rewrite those lines to say OpenAPI tags come from generator `Description.WithTags` (leaf Features namespace / `[OpenApiTags]`), FE `Tags()` remains filter metadata only, and `AutoTagPathSegmentIndex = 0` disables route-segment auto-tags so they do not compete with explicit OpenAPI tags.
- Status: open

### Issue 2 — Severity: suggestion
- File: `documentation/developer/reference/ApiEndpointSourceGenerator.md:79-97` (also OpenAPI section ~138–144)
- Description: Reference doc still shows generated `Configure()` with `Tags("WeatherForecast")` and a `Description` chain that has **no** `.WithTags(...)`, and still shows `AllowAnonymous()` as the default sample (pre-task-110). After this change, production emission pairs `Tags` with `WithTags`; the doc will mislead anyone implementing or debugging Scalar grouping from the reference page.
- Suggestion: Update the sample to emit both `Tags(...)` and `Description(d => d.WithTags(...).Produces...)` (or tags-only Description when no XML summary), drop the fail-open `AllowAnonymous()` default from the sample, and note that folder paths do not set tags—namespace leaf under `Features` does.
- Status: open

### Issue 3 — Severity: suggestion
- File: `tests/container-apps/api/api-server-integration-tests/` (missing coverage); related residual: web-server host
- Description: Regression protection for the original failure mode (Scalar UI up, no real document / no operation tags) is generator unit tests + a one-shot manual api-server curl recorded in the task. There is no durable host test that `GET /openapi/v1.json` returns 200 with feature tags on operations. `dev template-smoke` both matrices was not run (foundation package graph + template content changed). Web-server OpenAPI was not curl-proven standalone (Passwordless ApiSecret bare-host constraint noted in task Results).
- Suggestion: Add a thin api-server integration assertion on `/openapi/v1.json` (status + at least one operation tag e.g. `WeatherForecasts`). Run `dev template-smoke` both matrices before release. Optionally smoke web-server OpenAPI under Aspire/`dev run` once the host can start.
- Status: open

### Issue 4 — Severity: nit
- File: `source/container-apps/api/api-server/global-usings.cs:11`; `source/container-apps/api/api-server/api-server.csproj:19`
- Description: After moving Scalar mapping into `CommonServerModule`, api-server no longer references Scalar types directly, but still has `global using Scalar.AspNetCore` and a direct `Scalar.AspNetCore` PackageReference (types now come through foundation-server).
- Suggestion: Drop the unused global using; keep or drop the PackageReference only if dual-mode foundation packaging still requires it for package consumers (prefer transitive via `TimeWarp.Foundation.Server`).
- Status: open
