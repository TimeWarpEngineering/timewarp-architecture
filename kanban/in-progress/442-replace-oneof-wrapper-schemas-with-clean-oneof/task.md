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
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 1acdb70d-6b58-41ca-aa17-1fea632bb94d, max-turns 500, Done — 2026-10-29T14:22:28Z
- Review oracle, round 1 (ganda task work, 2026-10-29): review oracle claude-fable-5-1 (orchestrator,
  gates re-run, document dump); reviewer-general sub-agent claude-sonnet-5-5 (agent a648a9338e0772f73).
  Effort 2, roster general. Artifacts: `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`. Outcome: 0 bug, 3 open (M2, M3 suggestion; M5 nit), 2 wontfix (M1, M4).
  Fix pass requested for M2, M3, M5; next round under `review/round-2/`.

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

### Review (round 1, 2026-10-29)

Disposition: **not yet closed — fix pass requested** (round 1 ledger `review/round-1/merged.md`).

- Rounds: 1 so far. Roster: general (claude-sonnet-5-5) plus orchestrator verification. Effort 2.
- Counts after merge: bug 0 open; suggestion 2 open (M2 test pins every contract operation's `default`
  and the 4xx rewrite; M3 loud guard instead of silent no-op), 1 wontfix (M1, refuted against the real
  document); nit 1 open (M5 comment wrap and "when absent" wording), 1 wontfix (M4 nullable OneOf
  property, no wire path).
- Orchestrator re-ran the gates: `dev build` 0 warnings / 0 errors; web-server-integration-tests 415
  passed, 1 skipped, 0 failed; `OpenApiOneOf_` 6 passed. A dump of the real web document showed 167
  schemas, 0 `OneOf*` components or refs, and all 71 contract operations with `200 → …_Response` and
  `default → SharedProblemDetails`, which confirms the Results numbers above.
- `review/disposition.md` is written when the next round reaches 0 open.

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

## Learnings

- Restatement vs task (review round 1): the checklist says "Emit clean `oneOf: [Response,
  SharedProblemDetails]` (+ problem+json content type where apt)", while the implementer restatement
  publishes `200 → Response` and the problem arm under `application/problem+json` (`default`), with
  `oneOf` only where a union really is one-of. The two differ on the literal 200 shape. The review judges
  this a deliberate, recorded deviation rather than a misread: a 200 never carries a problem
  (`BaseFastEndpoint`), so the literal checklist shape would misdescribe the wire, and the task's own
  "problem+json where apt" clause points the same way. Recorded so the checklist wording and the shipped
  shape are not mistaken for each other later.
- Reviewer claims about generator and FastEndpoints behaviour (explicit `Produces(200)` suppressing the
  OneOf 200) were right in the abstract but did not reproduce in the real document; the review dumped the
  document before accepting the finding. Verify a predicted document shape against the generated document,
  not against decompiled library logic alone.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 71 OneOf wrappers of
238 schemas, 2026-10-28.
