# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general (reviewer-general-r2, claude-sonnet-5-5, agent ac106983bae88f949), orchestrator verification (review oracle, claude-fable-5-1)
**Scope:** delta since round 1 closed, `3848a0aec..HEAD` (commit `0b63e2c89`), product files only:
`source/foundation/foundation-server/open-api-one-of.cs`, `source/foundation/foundation-server/common-server-module.cs`,
`tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs` (177 insertions, 24 deletions).

## Counts

Final statuses across both rounds (round 1 ids carried, round 2 id added):

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 1 |
| nit | 0 | 1 | 2 |

## Orchestrator verification

Gates re-run in this worktree by the review oracle on `0b63e2c89`, not taken from the implementer's report:

- `./bin/dev build`: Build succeeded, 0 Warning(s), 0 Error(s).
- web-server-integration-tests `--filter-class OpenApiOneOf_`: 9 total, 9 passed, 0 failed.
- web-server-integration-tests, full suite: 419 total, 418 passed, 1 skipped, 0 failed.
- `./bin/dev template-smoke` (default variant): Template smoke SUCCEEDED (generated web 441/441, api 9/9,
  testing 14/14, co-located standalone runfiles 5/5, 5/5, 2/2).
- api-server-integration-tests (Aspire lane) not re-run: workers do not start an AppHost (same posture as
  round 1). The transformer is host-neutral and the api contracts have no request-class `<summary>`, so
  they take the same generated path the web document exercises.

## Prior findings (carried from round 1)

### M1 — Severity: suggestion — Status: wontfix
- Unchanged from round 1 (refuted against the generated document). The loud-guard half is M3.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs:133-192, :380-413
- Verified: `Every_Problem_Arm_Endpoint_Should_Publish_Default_Problem` enumerates the host's route endpoints,
  keeps those whose `EndpointDefinition.ResDtoType` is a OneOf with a `SharedProblemDetails` arm, normalises
  `{Name:guid}` to `{Name}` (every parameterised `[ApiRoute]` in `source/` uses that form), and fails closed:
  a path miss, a missing `default`, or a wrong `$ref` lands in `missing`. `OneOfProbeProblemStatusEndpoint`
  (`ProducesProblem(422)`) reaches the 4xx rewrite loop and the test pins the 422 `$ref`;
  `OneOfProbeNoProblemEndpoint` pins `oneOf` at 200 and no `default`. 9 tests pass in this worktree.
- Source: general
- Disposition notes: fixed in `0b63e2c89`. Verified by reviewer-general-r2 and the orchestrator's test run.

### M3 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/open-api-one-of.cs:100-116, :164-172, :17-19, :26-32
- Verified: a missing `asT{n}` member throws `InvalidOperationException` naming the OneOf type and member
  (a null `Properties` now also throws instead of returning). A problem-arm endpoint with no matching 2xx
  union, or a null `Responses`, throws naming method, route, response type and arm count. The Design
  region says both guards throw and why. No in-repo endpoint hits either throw: the full web suite fetches
  the document with the guards in place, `DownloadFeedbackAttachmentEndpoint` takes the normal OneOf path,
  and the Entra challenge endpoint is not a OneOf endpoint.
- Residual quiet path, recorded only: when one 2xx media type matches the union and another 2xx media type
  does not, the second keeps its union and nothing throws. FastEndpoints publishes one media type per
  response, so no in-repo shape reaches it. Not a finding.
- Source: general
- Disposition notes: fixed in `0b63e2c89`.

### M4 — Severity: nit — Status: wontfix
- Unchanged from round 1 (no OneOf-typed property can be on the wire today).

### M5 — Severity: nit — Status: fixed
- File: source/foundation/foundation-server/common-server-module.cs:29-32; open-api-one-of.cs:25
- Verified: the paragraph is re-wrapped (longest line 92 characters against 69-89 for neighbours), the
  OpenApiPublicServer sentence no longer reads as following OneOf, and the Design says "on default when the
  operation has none", matching the `ContainsKey` guard.
- Source: general
- Disposition notes: fixed in `0b63e2c89`.

## New findings (round 2)

### N1 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs:164
- Description: `checkedOperations.Count.ShouldBeGreaterThan(1, "No endpoint with a SharedProblemDetails arm
  was found.")` is satisfied by the two probe endpoints alone, so the bound does not by itself prove a
  contract endpoint was scanned.
- Suggestion (reviewer): assert a contract label such as `POST /Analytics/TrackEvent` is present, or require
  the count to exceed the probe count.
- Source: general
- Disposition notes: **wontfix** — the gap the bound leaves cannot occur without a sibling test failing on the
  same host. The probes and the contracts are discovered by the same FastEndpoints mechanism, so losing
  `EndpointDefinition` metadata drops both and the count falls to 0. The only way the contracts alone vanish
  from the scan is the test host's assembly filter changing, and then
  `Contract_Operation_Should_Publish_Response_And_Problem_Json` fails with "Missing post /Analytics/TrackEvent"
  because the contract is not in the document either. The bound is a sanity check on an otherwise fail-closed
  test, and the assertion message is accurate about what it guards. Decided by: review oracle, 2026-10-29.

## Duplicates / conflicts

- None. The reviewer raised one new item; the orchestrator agrees it is a nit and recorded the wontfix
  rationale above. No severity disagreement.
