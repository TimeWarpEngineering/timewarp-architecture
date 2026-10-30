# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None raised.

## Orchestrator verification

- `dotnet build tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj`: 0 warnings, 0 errors.
- `web-server-integration-tests --filter-class OpenApiOperationTags`: total 2, failed 0, succeeded 2. FastEndpoints registered 74 endpoints in the test host, so the guard scans the full web-server surface.
- Hand-written set is complete: the only non-generated `override void Configure()` under `source/container-apps` are the five edited files. `source/container-apps/api` has no hand-written endpoint source; every api-server endpoint is generated and gets the namespace-leaf tag. Checklist item "Check for other tag gaps" holds.
- Tag names match the generator: `GenerateEndpointClass` emits `Description(d => d.WithTags(...))` from the leaf namespace under `Features`; the five files declare `TimeWarp.Architecture.Features.Feedback` / `...Features.Identity`.
- `AutoTagPathSegmentIndex = 0` in `CommonServerModule` means explicit `WithTags` is the only tag source, so the guard cannot be satisfied by an auto-tag.
- Repeated `Description(...)` calls compose (`Delegate.Combine` on `UserConfigAction` in FastEndpoints 8.3.0); none of the five endpoints relied on that anyway, since `SignOutAntiforgeryTokenEndpoint` uses `Summary(...)` for its existing OpenAPI text.
- Not raised: generated siblings also call FastEndpoints `Tags(...)` (endpoint-filter metadata) alongside `Description.WithTags`; the five hand-written endpoints add only the OpenAPI tag. Nothing in `source/` filters FastEndpoints by tag (`Endpoints.Filter`, `EndpointTags`), so the omission has no effect and does not touch the task's acceptance criteria.
- Restatement vs. task: the `## Goal (implementer restatement)` matches the original task (five named operations, Feedback/Identity tags, Scalar grouping, other gaps checked). No misread.

## Duplicates / conflicts

None (single reviewer).
