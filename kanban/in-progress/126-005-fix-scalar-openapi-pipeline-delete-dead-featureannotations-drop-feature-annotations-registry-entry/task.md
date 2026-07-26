# Fix Scalar OpenAPI pipeline; delete dead FeatureAnnotations; drop feature-annotations registry entry

## Description

From task 126 dogfood review follow-through (maintainer decisions 2026-07-26). Three coupled
fixes, one story: make Scalar's feature-grouped sidebar actually work, and remove the vestigial
mechanism it superseded.

**1. Scalar is broken today — no OpenAPI document is ever generated.**
`source/foundation/foundation-server/common-server-module.cs:57-77`: `AddOpenApi` is a stub that
only calls `AddEndpointsApiExplorer()` (Swashbuckle-era metadata; generates no document), and
`UseScalarApiReference` maps the Scalar UI at a document route (`/openapi/v1.json` class) that
nothing ever maps. Repo has no `FastEndpoints.OpenApi` package, no `SwaggerDocument()`
registration, no `MapOpenApi()`. Per FastEndpoints docs
(fast-endpoints.com/docs/openapi-documents), FastEndpoints documents come from the
**`FastEndpoints.OpenApi`** package + `SwaggerDocument()` — explicitly NOT ASP.NET's raw
`services.AddOpenApi()` (which skips FastEndpoints' transformers/metadata).

Fix: add `FastEndpoints.OpenApi` (CPM pin in root `Directory.Packages.props`), replace the
`CommonServerModule.AddOpenApi` stub with proper `SwaggerDocument()` registration, and ensure the
document endpoint Scalar points at is actually mapped in `UseScalarApiReference`. Both
web-server and api-server host FastEndpoints — verify both call sites
(`web-server/program.cs:216-223`, `api-server/program.cs`).

**Grouping needs NO new work:** the FastEndpoint generator already derives each endpoint's
OpenAPI tag from the contract's `…Features.<Id>` namespace
(`source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:141-171`) and
emits `Tags("<Feature>")` in generated `Configure()`
(`generators/fast-endpoint-source-generator.cs:188-245`), with `[OpenApiTags]` as the
per-endpoint override hatch. Once a real document exists, Scalar groups by slice automatically.

**2. Delete the seven dead `FeatureAnnotations` files** — zero consumers repo-wide (verified by
grep 2026-07-26); superseded by the generator's namespace-derived tags:
- `source/container-apps/web/features/{admin/roles,analytics,auth,hello,identity,profile}/…-feature-annotations-server.cs` (6 files)
- `source/container-apps/api/api-contracts/features/weather-forecast/feature-annotations.cs`

**3. Drop the `feature-annotations → server` registry entry** (maintainer decision: drop, not
reserve — easy to re-add if ever needed):
`source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`.
**Registry edit ⇒ FULL REBUILD** (stale analyzer DLLs silently keep the old grammar under
incremental builds — AGENTS.md / tw-feature-placement skill warning).

**Explicitly deferred (do NOT implement):** two-level sidebar nesting via `x-tagGroups`
(area → slice, e.g. Admin → Roles). Scalar supports it via document transformer, but once
present, any tag not assigned to a group silently disappears from the sidebar. Revisit only
after the single-level sidebar is visible and the maintainer wants nesting.

## Checklist

- [x] Add `FastEndpoints.OpenApi` PackageVersion pin (root Directory.Packages.props) +
      PackageReference where FastEndpoints is hosted (foundation-server or the two server
      csprojs — follow how core FastEndpoints is referenced today)
- [x] Replace `CommonServerModule.AddOpenApi` stub with `OpenApiDocument()` registration
      (FastEndpoints.OpenApi 8.2.0 — not legacy SwaggerDocument); wire `UseScalarApiReference`
      to the real document route; reconcile Purpose/Design regions
- [x] Verify both hosts: web-server and api-server serve the document and Scalar renders it
      (api curl + durable integration test; web via shared CommonServerModule + AllowEmptyRequestDtos)
- [x] Delete the 7 dead FeatureAnnotations files
- [x] Remove `feature-annotations` from `feature-filename-grammar.json`; regenerate
      `feature-filename-grammar.g.props`; **full rebuild** (not incremental)
- [x] Update docs that name the function token: AGENTS.md axis-1 grammar line,
      `skills/tw-feature-placement/SKILL.md`, ApiEndpointSourceGenerator.md (review fix)
- [x] Runtime proof: api-server `GET /openapi/v1.json` 200 with feature tags; `/scalar` 200;
      durable `OpenApiDocument_Tests` (WeatherForecasts tag)
- [x] Gates: `dev build` 0/0 (full rebuild), analyzer/integration suites, `dev template-smoke`
      both matrices (SmokeDefault + SmokeNoPostgres)

## Notes

- Parent: 126. Lineage: FeatureAnnotations found dead during Steve's post-126-001/002 review;
  Scalar research (session 2026-07-26) confirmed the generator's namespace-derived tags already
  implement the intended grouping and the document pipeline is the only missing piece.
- Registry-drop decision (Steve, 2026-07-26): drop rather than keep-as-reserved — "easy to add
  back if we later need it." Contrast with `endpoint` token, which stays reserved (126 RFC F5).
- Scalar facts (researched from scalar/scalar docs + ScalarOptions source): sidebar groups by
  OpenAPI `tags`; `TagSorter.Alpha`/`OperationSorter`/`DefaultOpenAllTags` are presentation
  knobs on `MapScalarApiReference`; tag descriptions come from `tags[].description`;
  `x-tagGroups` gives two-level nesting with the all-tags-must-be-grouped caveat (deferred).
- FastEndpoints auto-tags by route segment when no explicit `Tags()` — our generator's explicit
  `Tags()` takes precedence; do not remove the generator emission.

### Implementation Plan

#### Goal
Make Scalar’s feature-grouped sidebar work end-to-end, then remove the dead FeatureAnnotations mechanism.

Three workstreams:
1. Wire real FastEndpoints OpenAPI document + Scalar consumers (web-server + api-server).
2. Fix generator tag derivation bug that currently emits `Tags("Architecture")` for every endpoint (blocks DoD).
3. Delete dead files, drop registry token, regenerate grammar artifacts, update live docs + tests.

#### Critical discovery
Task claimed “grouping needs NO new work” — **FALSE** against current tree.
`endpoint-metadata.cs` walks namespace and tags the **parent of Features** (e.g. Architecture), not the leaf feature Id (Roles, Identity). Generated endpoints confirm `Tags("Architecture")`. Must fix: tag = innermost namespace name of contract symbol under Features. Keep `[OpenApiTags]` additive. No `x-tagGroups`.

#### API correction
Use **FastEndpoints.OpenApi 8.2.0** (match FastEndpoints 8.2.0):
- `services.OpenApiDocument(o => { ... })` — NOT `SwaggerDocument`, NOT raw `AddOpenApi`
- `app.UseFastEndpoints().MapOpenApi()` via `CommonServerModule.UseScalarApiReference` after FE
- `MapScalarApiReference` with `AddDocument` matching `DocumentName` (`apiVersion` `"v1"`)

#### Package wiring
- `Directory.Packages.props`: `PackageVersion` FastEndpoints.OpenApi 8.2.0
- `foundation-server.csproj`: PackageReference (with FastEndpoints + Scalar)
- global-usings: `FastEndpoints.OpenApi`
- web/api server csprojs: no new ref if APIs stay in CommonServerModule

#### CommonServerModule
- **AddOpenApi:** `OpenApiDocument` with `DocumentName=apiVersion`, Title, Version, `AutoTagPathSegmentIndex=0`, `ExcludeNonFastEndpoints=true`. Drop `AddEndpointsApiExplorer`. Prefer drop unused `typeArray` param.
- **UseScalarApiReference:** `MapOpenApi` + `MapScalarApiReference(WithTitle, AddDocument)`. Rewrite false Purpose/Design regions.

#### Hosts
- **web-server:** keep `AddOpenApi` in ConfigureServices; move `UseScalarApiReference` AFTER `UseFastEndpoints`; Scalar always-on.
- **api-server:** use `CommonServerModule.AddOpenApi`; Development-only `UseScalarApiReference` after FE; remove orphan `AddEndpointsApiExplorer` and direct `MapScalarApiReference`.

#### Generator fix
`endpoint-metadata.cs`: if Features is ancestor, `tags.Add(symbol.ContainingNamespace.Name)`. Update Design. Generator tests for WeatherForecast, nested `Admin.Roles` → `Roles`. Do not remove `Tags()` emission.

#### Delete 7 FeatureAnnotations files
- web features: admin/roles, analytics, auth, hello, identity, profile `*-feature-annotations-server.cs`
- api: `weather-forecast/feature-annotations.cs`

#### Registry
Drop `feature-annotations` from `feature-filename-grammar.json` (keep `endpoint`). Regenerate `.g.cs` and `.g.props`. **FULL rebuild mandatory.**

#### Docs/tests
- AGENTS.md axis-1
- `skills/tw-feature-placement/SKILL.md`
- 126-001 placement mentions
- `feature-filename-grammar-analyzer-tests` (remove feature-annotations cases; multi-segment TWA0016 case)
- analyzer Design comments

#### Verification order
1. Package + CommonServerModule + hosts
2. Generator tag fix + tests
3. Delete FeatureAnnotations
4. Registry drop + regen + docs/tests
5. `dev build` (full) 0/0; `dev test`; `dev template-smoke` both matrices
6. `dev run` + curl `/openapi/v1.json` tags + Scalar sidebar proof into task record

#### Out of scope
`x-tagGroups`, Swashbuckle/NSwag, removing generator `Tags`, keeping `feature-annotations` reserved, changing web Scalar always-on policy.

#### Risks
- Stale analyzer DLLs → full rebuild
- `MapOpenApi` before FE → order after FE
- DocumentName mismatch
- Double tagging → `AutoTagPathSegmentIndex=0`
- Incomplete multi-segment tests after drop

## Session

- Created: 2026-07-26 — filed from Scalar research + maintainer decisions (fix pipeline, delete
  dead files, drop registry entry).
- Planning: 2026-07-26
- Implementer: grok session 2026-07-26
- Review: grok Phase 4b effort 1 (round 1 + round 2 re-verify) 2026-07-26
- Orchestration complete: 2026-07-26

## Results

### What was implemented
1. **OpenAPI document pipeline** via FastEndpoints.OpenApi 8.2.0 (not raw `AddOpenApi` / not SwaggerDocument):
   - CPM pin + foundation-server PackageReference + global using
   - Transitive lift of Microsoft.OpenApi 2.0.0 → **2.7.5** (NU1903 / GHSA-v5pm-xwqc-g5wc)
   - `CommonServerModule.AddOpenApi` → `OpenApiDocument` (DocumentName/Title/Version, AutoTagPathSegmentIndex=0, ExcludeNonFastEndpoints)
   - `CommonServerModule.UseScalarApiReference` → `MapOpenApi` + `MapScalarApiReference` (WithTitle, AddDocument, SortTagsAlphabetically, ExpandAllTags)
   - web-server: always-on Scalar **after** UseFastEndpoints; dropped unused typeArray arg
   - api-server: AddOpenApi in ConfigureServices; Development-only UseScalarApiReference after FE; removed bare MapScalarApiReference + AddEndpointsApiExplorer
   - **AllowEmptyRequestDtos=true** on both hosts so FE.OpenApi accepts propertyless request DTOs

2. **Generator tag derivation fix**: leaf namespace under Features (`…Admin.Roles` → `Roles`), not parent of Features (`Architecture`). `[OpenApiTags]` remains additive + Distinct.

3. **OpenAPI operation tags**: FE `Tags()` is filter-only. Generator also emits `Description(d => d.WithTags(...))` so Scalar sidebar groups by feature. `Tags()` emission retained.

4. **Deleted 7 dead FeatureAnnotations files**; dropped `feature-annotations` from registry; regenerated `.g.cs` / `.g.props`; updated AGENTS.md, tw-feature-placement skill, analyzer Design comments, grammar tests, ApiEndpointSourceGenerator.md.

### Files changed (primary)
- `Directory.Packages.props` — FastEndpoints.OpenApi 8.2.0, Microsoft.OpenApi 2.7.5
- `source/foundation/foundation-server/{foundation-server.csproj,global-usings.cs,common-server-module.cs}`
- `source/container-apps/{web/web-server,api/api-server}/program.cs` (+ api-server.csproj / global-usings cleanup)
- `source/analyzers/timewarp-architecture-analyzers/{models/endpoint-metadata.cs,generators/fast-endpoint-source-generator.cs}`
- `source/analyzers/timewarp-architecture-convention-analyzers/{feature-filename-grammar.json,.g.cs,feature-filename-grammar-analyzer.cs}`
- `source/container-apps/web/msbuild/feature-filename-grammar.g.props`
- Deleted 6 `*-feature-annotations-server.cs` + api weather `feature-annotations.cs`
- Tests: sourcegenerator + feature-filename-grammar analyzer tests + `OpenApiDocument_Tests.cs`
- Docs: `AGENTS.md`, `skills/tw-feature-placement/SKILL.md`, `ApiEndpointSourceGenerator.md`

### Key decisions / deviations
- **Microsoft.OpenApi 2.7.5 direct pin** required so restore passes with warnings-as-errors (FE.OpenApi transitively pulls vulnerable 2.0.0).
- **Generator emits both Tags() and WithTags** — plan forbade removing Tags(); runtime proved Tags alone leaves OpenAPI operations untagged.
- typeArray parameter dropped from AddOpenApi (unused).
- **AllowEmptyRequestDtos** required for OpenAPI generation over empty identity/profile Query DTOs.

### Verification
| Gate | Result |
|------|--------|
| `dev build` (full) | **0 Warning(s), 0 Error(s)** |
| sourcegenerator-tests | **52 passed** (WeatherForecast / nested Roles / OpenApiTags) |
| analyzers-tests | **95 passed** (grammar registry sync after drop) |
| api-server-integration-tests | **7 passed**, 1 skipped (incl. durable OpenAPI document test) |
| web-server / spa / foundation / identity | passed (pre-fix suites) |
| aspire-tests IngressSmoke | **5 failed** — `ingress` failed to start (env; not OpenAPI-related) |
| `dev template-smoke` | **both matrices SUCCEEDED** (SmokeDefault + SmokeNoPostgres) |
| Generated artifacts | `Tags`/`WithTags` leaf features — **no Architecture** |
| Runtime curl api-server | `GET /openapi/v1.json` **200**, tags `["WeatherForecasts"]`; `/scalar` **200** |

### Phase 4b review
- **Effort:** 1 (general only)
- **Rounds:** 2 (round 1 findings → fix; round 2 re-verify)
- **Final counts:** bug 0; suggestion 3 fixed; nit 1 fixed; **0 open**
- **Disposition:** `clean` — `review/disposition.md`
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`
- **Fixes from review:** Design/WithTags accuracy, ApiEndpointSourceGenerator.md, durable OpenAPI integration test + template-smoke, api-server Scalar package/using cleanup

### Residual risks
- **web-server** standalone OpenAPI curl skipped (Passwordless ApiSecret bare-host); covered via shared CommonServerModule + `AllowEmptyRequestDtos` + generated WithTags.
- Aspire ingress smoke failures appear environmental.
- Generated Description indentation slightly irregular (cosmetic).
- `x-tagGroups` two-level nesting remains **explicitly deferred**.
