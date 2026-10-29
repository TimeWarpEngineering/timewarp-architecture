# Review framework

## Budget (by-diff)

- Lines changed: 259
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 441

**Date:** 2026-10-29
**Host task:** kanban/in-progress/441-add-missing-tags-on-5-operations/
**Diff scope:** commit f29bd23a4 on branch `task/441-add-missing-tags-on-5-operations` (five `*-endpoint-server.cs` files under `source/container-apps/web/features/{feedback,identity}/` plus new `tests/container-apps/web/web-server-integration-tests/open-api-operation-tags-tests.cs`). The branch is behind master, so `master...HEAD` also shows already-merged sibling tasks; those are out of scope.
**Plan / brief:** task.md `## Goal (implementer restatement)` — tag the five hand-written FastEndpoints with their generated siblings' namespace-leaf tag and add a guard test that fails on any untagged web-server OpenAPI operation.
**Effort:** 2 (general reviewer sub-agent + orchestrator verification)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-fable-5-1) under `ganda task work`; general reviewer sub-agent a8e57b9543f31cee2 (claude-sonnet-5-5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
