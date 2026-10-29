# Review framework

## Budget (by-diff)

- Lines changed: 597
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
