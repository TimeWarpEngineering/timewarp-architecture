# Review framework

## Budget (by-diff)

- Lines changed: 49
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 1 framework — task 443

**Date:** 2026-10-29
**Host task:** kanban/in-progress/443-clarify-requestagenthumanlink-humanprincipalid-semantics/
**Diff scope:** implementation commit `d0f0dc2e4` on branch `task/443-clarify-requestagenthumanlink-humanprincipalid-sem` (the branch is a work-set stack; earlier tasks' commits are out of scope). Product files: `source/container-apps/web/features/agent-links/request-agent-human-link/request-agent-human-link-contracts.cs`, `source/container-apps/web/projects/web-spa/wwwroot/auth.md`, `source/container-apps/web/projects/web-spa/wwwroot/llms.txt`, `tests/container-apps/web/web-server-integration-tests/open-api-request-agent-human-link-tests.cs` (new).
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Results`: publish `humanPrincipalId` as required with a description and uuid example, leave runtime unchanged (Validator stays the rule), describe the optional human link in auth.md and llms.txt, pin the schema with a test.
**Effort:** 2 (host Budget.ByDiff, 264 lines) — roster axis general
**Reviewer roster:** general (reviewer-general sub-agent, claude-sonnet-5-5, read-only) + orchestrator gate re-run (build, new tests, repo audit) and claim verification
**Session IDs:** implement oracle session c5708c32-27fc-453a-b85b-abc0d8c7985c (claude-opus-5-5); review oracle: ganda task work review round 1, 2026-10-29, claude-fable-5-1; reviewer-general agent id a6b5c07a7a4582d67

## Round 2 framework — task 443

**Date:** 2026-10-29
**Diff scope:** fix commit `e6b00753c` vs round-1 base `7afc5224a` (round-1 `base.sha`). Product delta: `source/container-apps/web/projects/web-spa/wwwroot/auth.md` only.
**Plan / brief:** `review/round-1/merged.md` (M1 suggestion, M2 nit; fix pass requested).
**Effort:** 1 (host Budget.ByDiff, 49 lines) — roster axis general
**Reviewer roster:** general, performed inline by the review oracle (claude-fable-5-1); the delta is 15 doc lines, so no sub-agent was spawned
**Session IDs:** fix pass implement-fix-2 oracle (claude-opus-5-5, 2026-10-29); review oracle round 2, ganda task work, 2026-10-29, claude-fable-5-1

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
