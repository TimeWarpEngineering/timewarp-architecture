# Review framework

## Budget (by-diff)

- Lines changed: 457
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-07
**Host task:** kanban/in-progress/484-update-all-nuget-pins-to-latest-timewarpstate-1200-beta17-timewarpmediator-1500-beta5-opentelemetry-instrumentation-1202/
**Diff scope:** branch `task/484-update-all-nuget-pins-to-latest-timewarpstate-1200` vs `master` (commit 9f821bffe plus kanban moves)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: bump all outdated pins, fix new
TimeWarp.State diagnostics at the source, gates green.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Opus 5.5 (claude-opus-5-5), ganda task work headless, 2026-11-07

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
