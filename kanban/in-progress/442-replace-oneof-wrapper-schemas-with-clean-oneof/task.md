# Replace OneOf wrapper schemas with clean oneOf

## Description

71 of 238 schemas are `OneOfOneOfOf...` wrappers leaking FastEndpoints
internals (`value/index/isT0/isT1/asT0/asT1`). Works but noisy for codegen
and agent clients.

## Checklist

- [x] Emit clean `oneOf: [Response, SharedProblemDetails]` (+ problem+json content type where apt) instead of wrapper objects
- [x] Verify Scalar + generated clients still work
- [x] Wrapper count goes toward zero

## Goal (implementer restatement)

Contract endpoints declare `OneOf<Response, SharedProblemDetails>` as their FastEndpoints response
type, so the OpenAPI document publishes `OneOf…` component schemas with the library's
`value/index/isT0/asT0…` members. That is not the wire shape: `BaseFastEndpoint` writes success as
bare `Response` (`application/json`, 200) and failure as `SharedProblemDetails`
(`application/problem+json`, the problem's status). The document should say what the wire does.

Acceptance:

- No `OneOf…` wrapper schema remains in `components.schemas`, and no `$ref` points at one.
- A union that does reach a schema (a property or response typed `OneOf<A, B>`) is published as
  `oneOf: [A, B]`, not as the wrapper object.
- Contract operations publish 200 → `Response` as `application/json` and the problem response as
  `SharedProblemDetails` under `application/problem+json`.
- A web-server integration test scans the whole document and fails on the next wrapper.
- Scalar still serves (`/scalar/v1` 200) and the document still parses as OpenAPI JSON. The C#
  clients call contracts through `HttpApiService`, and the repo has no OpenAPI codegen (no NSwag,
  Kiota, or openapi-generator), so "clients still work" is checked by the build and the suites.

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implemented 2026-10-29 (implement oracle, ganda task work). arch.timewarp.work returned 502, so
  the evidence was reproduced from a local in-process document: 71 wrappers of 238 schemas, every
  one referenced from a 200 `application/json` response on a generated contract endpoint.

## Results

`OpenApiOneOf` (`source/foundation/foundation-server/open-api-one-of.cs`) is registered first in
`CommonServerModule.AddOpenApi`'s `ConfigureOpenApi`, after the FastEndpoints transformers. It
applies to the web and api documents both.

- Reference id: wraps the FastEndpoints `CreateSchemaReferenceId` and returns null for a OneOf type,
  so the union is inlined and never becomes a component. Other types keep their FastEndpoints names.
- Schema transformer: a OneOf schema becomes `oneOf` over its `asT0…` member schemas, which are
  already `$ref`s to the arm components. The wrapper members are dropped.
- Operation transformer: for an endpoint whose response type is a OneOf containing
  `SharedProblemDetails`, the 2xx content keeps the success arm (or `oneOf` over several success
  arms), and the problem arm is published as `default` under `application/problem+json` (plus any
  existing 4xx/5xx problem+json entry). `default` because the status is the problem's own (400 when
  the handler sets none), not one fixed code.

Spec deviation, on purpose: the 200 response is the bare `Response`, not `oneOf: [Response,
SharedProblemDetails]`. A 200 never carries a problem (`BaseFastEndpoint`), so the problem arm goes
to the problem+json response instead. That is the "problem+json where apt" half of the checklist.
`oneOf` appears where a union really is one-of: several success arms, or a OneOf-typed property.

Measured on the local web document: wrappers 71 → 0, schemas 238 → 167, `isT0` occurrences 0.
Every contract operation now has `200` → `…_Response` and `default` → `SharedProblemDetails`.

A OneOf-typed DTO property is published as `oneOf` too, but the contract JSON options have no OneOf
converter and the default serializer throws on the inactive `AsT` getter, so no such property can
be on the wire today. Recorded in the Design region, not a follow-up.

Tests: `tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs` (6 tests).
It scans the whole document for wrapper components and wrapper shapes, checks TrackEvent's 200 and
default, checks every `default` is SharedProblemDetails, and uses two probe endpoints for the
two-success-arm response and the OneOf property. Negative control: with the registration commented
out, 5 of 6 fail (Scalar stays green).

Gates run in this worktree:

- `dev build`: 0 warnings, 0 errors.
- web-server-integration-tests, full suite: 415 passed, 1 skipped, 0 failed.
- api-server-integration-tests `--filter-class OpenApi`: 1 passed.
- `ganda repo audit`: passes. One advisory `kanban-landed` warning lists other tasks' claims with
  no worktree (070-001, 104-024/025/026, 123, 141, 198, 244). Those are board state for other
  tasks, not this change, so they were left alone.
- `dev check-version`: 2.0.0-beta.29 in source, one ahead of nuget 2.0.0-beta.28. No bump needed.
- `dev template-smoke` (default variant): SUCCEEDED (generated web 441/441, api 9/9, testing 14/14).

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet run -- --filter-class OpenApiOneOf_
```

Expect: 6 passed, 0 failed. Then, on a running web-server (or arch.timewarp.work once deployed),
`curl -s <origin>/openapi/v1.json | jq '[.components.schemas | keys[] | select(startswith("OneOf"))] | length'`
prints `0`, and `jq '.paths["/Analytics/TrackEvent"].post.responses'` shows `200` →
`TimeWarpArchitectureFeaturesAnalyticsTrackEvent_Response` (application/json) and `default` →
`TimeWarpFoundationTypesSharedProblemDetails` (application/problem+json). `/scalar/v1` loads and
lists the operations with both responses.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 71 OneOf wrappers of
238 schemas, 2026-10-28.
