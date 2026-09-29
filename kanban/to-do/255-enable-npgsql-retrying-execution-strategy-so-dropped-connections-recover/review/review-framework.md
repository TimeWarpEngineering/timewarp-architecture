# Review framework — task 255

**Date:** 2026-09-29
**Host task:** kanban/to-do/255-enable-npgsql-retrying-execution-strategy-so-dropped-connections-recover/
**Diff scope:** branch `task/255-enable-npgsql-retrying-execution-strategy-so-dropp` vs `master` (commit c21dabe6)
**Plan / brief:** task.md Requirements — bounded `EnableRetryOnFailure` on the runtime `PostgresDbContext`, explicit transaction wrapped in the execution strategy, concurrency exceptions not retried, `pg_terminate_backend` recovery tests
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (Claude Code, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
