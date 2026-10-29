# Review framework

## Budget (by-diff)

- Lines changed: 265
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 1 framework — task 442

**Date:** 2026-10-29
**Host task:** kanban/in-progress/442-replace-oneof-wrapper-schemas-with-clean-oneof/
**Diff scope:** branch `task/442-replace-oneof-wrapper-schemas-with-clean-oneof` vs merge base `841e9b637` (PR #604 merge), product files only: `source/foundation/foundation-server/open-api-one-of.cs` (new), `source/foundation/foundation-server/common-server-module.cs`, `tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs` (new). Implementation commit `5b5985101`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Results`: publish OneOf response unions as their wire shape (200 → Response, problem → SharedProblemDetails under application/problem+json), no `OneOf…` wrapper component or `$ref`, whole-document scan test, Scalar still serves.
**Effort:** 2 (host Budget.ByDiff, 597 lines) — roster axis general
**Reviewer roster:** general (reviewer-general sub-agent, claude-sonnet-5-5, read-only) + orchestrator gate re-run and document dump
**Session IDs:** implement oracle session 1acdb70d-6b58-41ca-aa17-1fea632bb94d (claude-opus-5-5); review oracle: ganda task work review round 1, 2026-10-29, claude-fable-5-1; reviewer-general agent id a648a9338e0772f73

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 framework — task 442

**Date:** 2026-10-29
**Diff scope:** delta since round 1 closed, `3848a0aec..HEAD` (fix-pass commit `0b63e2c89`), product files only: `source/foundation/foundation-server/open-api-one-of.cs`, `source/foundation/foundation-server/common-server-module.cs`, `tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs` (177 insertions, 24 deletions). Round 1 findings M2, M3, M5 re-verified against this delta; M1 and M4 stay wontfix.
**Plan / brief:** round 1 ledger `review/round-1/merged.md` plus task.md `## Results` → "Fix pass (review round 1 findings)".
**Effort:** 2 (host Budget.ByDiff, 265 lines) — roster axis general
**Reviewer roster:** general (reviewer-general-r2 sub-agent, claude-sonnet-5-5, read-only, agent ac106983bae88f949) + orchestrator gate re-run (build, OpenApiOneOf_ and full web-server suite, template smoke)
**Session IDs:** fix-pass oracle: ganda task work implement-fix-2, 2026-10-29; review oracle round 2: reviewer-claude-fable (claude-fable-5-1), session 089264dd-6d97-4701-a33d-c1ddef4bf936
**Outcome:** 0 open; M2, M3, M5 fixed; N1 (nit) wontfix; disposition `accepted-exceptions` in `review/disposition.md`.
