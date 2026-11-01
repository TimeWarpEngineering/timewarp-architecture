# Review framework

## Budget (by-diff)

- Lines changed: 366
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-01
**Host task:** kanban/in-progress/460-remove-oakton-aspire-and-aspnet-health-checks-already-cover-startup-checks/
**Diff scope:** branch `task/460-remove-oakton-aspire-and-aspnet-health-checks-alre` vs merge-base `e126c27cd` with master (commits a44c4851d, 800bdf9df; kanban commits excluded)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: remove Oakton, run both hosts with `RunAsync`, delete the environment checks, keep `AddDbContextCheck<PostgresDbContext>`, narrow Oakton-only workarounds.
**Effort:** 2 (budget by diff)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
