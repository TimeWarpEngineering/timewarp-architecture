# Add missing tags on 5 operations

## Description

These ops have no `tags` (breaks Scalar grouping/codegen):
`POST /api/Feedback/attachments`,
`GET /api/Feedback/attachments/{attachmentId}`,
`GET /api/identity/entra/challenge`, `POST /identity/sign-out`,
`GET /identity/sign-out/antiforgery-token`.

## Goal (implementer restatement)

The five hand-written FastEndpoints do not go through the source generator, so they never get
the `Description.WithTags(...)` the generator emits from the namespace leaf. Their OpenAPI
operations ship with no `tags`, so Scalar puts them in no group and client codegen cannot group
them.

Acceptance criteria:

- Each of the five operations carries the same tag as its generated siblings: `Feedback` for the
  two attachment routes and `Identity` for the Entra challenge and both sign-out routes.
- A test fails if any web-server OpenAPI operation has no tag, so the next hand-written endpoint
  cannot regress this.
- No other untagged operations remain (web-server and api-server checked).

## Checklist

- [x] Add tags (Feedback, Identity, or appropriate group)
- [x] Verify Scalar groups render
- [x] Check for other tag gaps

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implemented under `ganda task work` (implement node), 2026-10-29.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 04b2d5c9-79ac-44b2-86b3-50fc076474c6, max-turns 500, Done — 2026-10-29T13:13:51Z
- Review oracle: tw-implementation-review, effort 2, roster general (sub-agent a8e57b9543f31cee2, claude-sonnet-5-5), orchestrator claude-fable-5-1, round 1, disposition clean — 2026-10-29
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session 0585e958-5bbd-4ffb-b328-4bc231dd548f, max-turns 120 — 2026-10-29T13:18:58Z

## Review

- Round 1: Done

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.

## Results

- Added `Description(... WithTags("Feedback"|"Identity"))` to the five hand-written endpoints
  (`source/container-apps/web/features/feedback/{upload,download}-feedback-attachment/*-endpoint-server.cs`,
  `source/container-apps/web/features/identity/challenge-entra/challenge-entra-endpoint-server.cs`,
  `source/container-apps/web/features/identity/sign-out-browser-session/*-endpoint-server.cs`).
  Each tag matches the namespace-leaf tag the generator gives that feature's other endpoints. Each
  Design region records why the tag is set by hand.
- New guard `tests/container-apps/web/web-server-integration-tests/open-api-operation-tags-tests.cs`
  boots every FastEndpoint type in the web-server assembly and fetches `/openapi/v1.json`. It
  asserts that every operation has at least one tag, and that the five routes carry exactly
  `Feedback` / `Identity`.
- Other gaps: without the fix the guard lists exactly these five operations, so the web-server
  has no other untagged operation. The api-server has no hand-written endpoints. Every api-server
  endpoint is generated, so each one gets the namespace-leaf tag.
- Scalar grouping: Scalar groups the sidebar by operation `tags` (`SortTagsAlphabetically` /
  `ExpandAllTags` in `CommonServerModule`). The five operations now sit under the existing
  Feedback and Identity groups instead of the untagged default.
- Negative proof: with the source edits reverted, the guard failed 2/2 and printed the five
  routes above. With the fix, all 36 `OpenApi*` test methods pass and the full
  web-server-integration-tests suite passes (386/386).

### Review

- Rounds: 1. Roster: general (effort 2). Disposition: **clean**, 0 open, 0 wontfix.
- Counts: bug 0 / suggestion 0 / nit 0.
- Reviewer re-ran the guard: build 0 warnings, `--filter-class OpenApiOperationTags` 2/2 passed over 74 registered endpoints. Confirmed the five files are the only hand-written `Configure()` overrides under `source/container-apps`, the api-server has none, and the tag names match the generator's namespace-leaf emission. Restatement matches the original task; no misread.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
dotnet build tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj
tests/container-apps/web/web-server-integration-tests/bin/Debug/net11.0/web-server-integration-tests --filter-class OpenApiOperationTags
```

Expect: `total: 2`, `failed: 0`, `succeeded: 2`. After deploy,
`curl -s https://arch.timewarp.work/openapi/v1.json | jq '[.paths[][] | select((.tags // []) | length == 0)] | length'`
prints `0`, and Scalar shows the attachment routes under **Feedback** and the challenge and
sign-out routes under **Identity**.
