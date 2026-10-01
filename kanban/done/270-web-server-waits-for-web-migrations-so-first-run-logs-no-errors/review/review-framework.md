# Review framework

## Budget (by-diff)

- Lines changed: 528
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 270

**Date:** 2026-10-02
**Host task:** kanban/to-do/270-web-server-waits-for-web-migrations-so-first-run-logs-no-errors/
**Diff scope:** branch `task/270-web-server-waits-for-web-migrations-so-first-run-l` vs `origin/master` (12 files, +485/−43)
**Plan / brief:** task.md Requirements — re-test WaitForCompletion on Aspire 13.6 (rejected with evidence), fallback quiet table probe before the boot seed's EF read, closed-box + Postgres tests, Design regions reconciled
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle — Claude Opus 5.5 (ganda task work, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
