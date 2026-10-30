# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general (reviewer-general, claude-sonnet-5-5), orchestrator verification (review oracle)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 1 |
| nit | 1 | 0 | 1 |

## Orchestrator verification

Gates re-run in this worktree by the review oracle (not taken from the implementer's report):

- `dev build` (`./bin/dev build`): Build succeeded, 0 Warning(s), 0 Error(s).
- web-server-integration-tests, full suite: total 416, succeeded 415, skipped 1, failed 0.
- web-server-integration-tests `--filter-class OpenApiOneOf_`: 6 passed.
- api-server-integration-tests `--filter-class OpenApi` was **not** re-run here: it is the Aspire closed-box
  lane and this review did not launch an AppHost from a worker. The implementer reports 1 passed. The
  transformer is host-neutral (shared `CommonServerModule.AddOpenApi`), so the web document covers it.
- Real web document, dumped from a throwaway host built the same way the `OpenApiOneOf_` test builds its
  host (web-server endpoints, `CommonServerModule` pipeline), inspected outside the test:
  schemas 167, `OneOf*` components 0, `isT0` occurrences 0, `$ref`s to a `OneOf*` schema 0,
  operations 74, of which 71 contract operations all publish `200 → …_Response` (`application/json`)
  and `default → TimeWarpFoundationTypesSharedProblemDetails` (`application/problem+json`). The 3
  operations without `default` are hand-written non-contract endpoints (`GET /api/identity/entra/challenge`,
  `POST /identity/sign-out`, `GET /identity/sign-out/antiforgery-token`). No contract operation carries a
  `400`; the only `400` in the document is the hand-written Entra challenge. No unreferenced component.

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/foundation/foundation-server/open-api-one-of.cs:141-157
- Description: The problem arm is discovered only from a 2xx media type whose schema is a `oneOf` with
  `arms.Length` members; when none is found the transformer returns before the 4xx/5xx rewrite and before
  adding `default`. The reviewer raised this as a **bug**, reasoning that a generated endpoint whose request
  class has an XML `<summary>` goes through the generator's
  `Produces<Response>(200).ProducesProblem(400)` chain (fast-endpoint-source-generator.cs:439), that
  FastEndpoints' `ProducesDeDuped` then skips its own OneOf 200 (confirmed by decompiling
  FastEndpoints 8.3.0), and that such operations would therefore keep ASP.NET `ProblemDetails` at 400 with
  no `default`.
- Verification: six web contracts do have a `<summary>` directly on the request class
  (`agent-chat/*xai-api-key*` Command/Query classes). In the real document every one of those six
  operations publishes `200 → …_Response` and `default → SharedProblemDetails` and has **no** `400`
  (see Orchestrator verification). The predicted failure does not occur for any operation in this repo, so
  the finding is not a defect of this change. Downgraded from bug to suggestion.
- Suggestion (kept for the record): resolving the problem schema from the type
  (`context.GetOrCreateSchemaAsync(typeof(SharedProblemDetails), …)`) instead of from the 2xx union would
  make the transformer independent of how the 200 was declared.
- Source: general
- Disposition notes: **wontfix** — refuted against the generated document; no in-repo operation reaches the
  early return. The loud-guard part of the concern is carried by M3. Decided by: review oracle, 2026-10-29.

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs:124
- Description: The tests do not pin the acceptance criterion "every contract operation publishes the
  problem response". `Every_Default_Response_Should_Be_Shared_Problem_Details` asserts only `defaults > 0`
  and only checks `default`s that exist, so an operation that silently lost its `default` (the M1 shape)
  would pass. The 4xx/5xx rewrite loop (open-api-one-of.cs:154-163) has no test and, per the document dump,
  no in-repo contract operation exercises it (no contract operation has a 4xx `application/problem+json`
  entry). A OneOf response without a `SharedProblemDetails` arm (should stay `oneOf` at 200, no `default`)
  is also uncovered.
- Suggestion: (a) assert that every operation whose 200 schema is a `…_Response` `$ref` (or every operation
  of a FastEndpoints endpoint whose `ResDtoType` is a OneOf containing `SharedProblemDetails`) has
  `default → SharedProblemDetails`; (b) add a probe endpoint that declares an explicit
  `Description(d => d.ProducesProblem(422))` (or similar 4xx problem+json) and assert its 4xx schema is
  rewritten to `SharedProblemDetails`; (c) optionally a probe `OneOf<A, B>` without a problem arm that
  asserts `oneOf` at 200 and no `default`.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/foundation/foundation-server/open-api-one-of.cs:98-101 and :141
- Description: Both guards no-op silently: a missing `asT{n}` member returns without rewriting
  (lines 98-101), and `union.Count != arms.Length` skips the split (line 141). Because
  `CreateSchemaReferenceId` already returns null for every OneOf, an unrewritten wrapper is then inlined at
  each use with `value/index/isT0/asT0` members and no diagnostic. A changed serializer naming policy or a
  OneOf package change would publish wrappers again without failing anything but the scan test (whose
  member names are also fixed strings). Repo posture (AGENTS.md): when two things must agree, add a check.
- Suggestion: fail loudly (throw `InvalidOperationException` naming the OneOf type / endpoint) when an
  `asT{n}` member is missing or when an endpoint whose `ResDtoType` contains `SharedProblemDetails` has no
  matching union at 2xx. The `OpenApiOneOf_` tests then fail with the cause instead of a wrapper scan hit.
  Alternatively log at warning if a 500 on `/openapi/v1.json` is judged too aggressive; say which in the
  Design region.
- Source: general (orchestrator folded the loud-guard half of M1 into this id)
- Disposition notes:

### M4 — Severity: nit — Status: wontfix
- File: source/foundation/foundation-server/open-api-one-of.cs:106-109
- Description: `Arms()` unwraps `Nullable<T>`, so a nullable OneOf property reaches the schema
  transformer, which then sets `schema.Type = null`; a `type: ["object","null"]` on the wrapper would lose
  its `null`. Not verified by running.
- Source: general
- Disposition notes: **wontfix** — no OneOf-typed DTO property can be on the wire today (the contract JSON
  options register no OneOf converter and the default serializer throws on the inactive `AsT` getter), and
  the Design region already records that the property rule is wrapper hygiene only. Revisit if a OneOf
  converter is ever added. Decided by: review oracle, 2026-10-29.

### M5 — Severity: nit — Status: open
- File: source/foundation/foundation-server/common-server-module.cs:30; source/foundation/foundation-server/open-api-one-of.cs:23-25
- Description: The inserted Design sentence leaves common-server-module.cs line 30 at 114 characters
  (neighbouring lines wrap at 85-88), and the following "OpenApiPublicServer then replaces servers" now
  reads as if it follows OneOf rather than the bearer step. In open-api-one-of.cs the Design says the
  problem arm is published "on default", but the code adds `default` only when absent (line 166) and never
  rewrites an existing one.
- Suggestion: re-wrap the comment at the file's usual width and add "when absent" to the `default` clause.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- The reviewer's Issue 1 (bug) and Issue 3 (suggestion) overlap on "silent early return / silent guard".
  The defect claim in Issue 1 was refuted against the real document and recorded as M1 wontfix; the
  robustness half is carried by M3 so it is not lost.
- No conflicting severities between sources; the orchestrator's only severity change is M1 bug → suggestion,
  with the evidence above.
