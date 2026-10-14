# Review framework

## Budget (by-diff)

- Lines changed: 556
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 363

**Date:** 2026-10-14
**Host task:** kanban/to-do/363-web-server-boot-waits-for-no-pending-migrations-instead-of-only-the-site-settings-table-existing/
**Diff scope:** branch `task/363-web-server-boot-waits-for-no-pending-migrations-in` vs `master` (db9048e04)
**Plan / brief:** Replace task 270's site-settings table probe with `IPendingMigrationsProbe` / `EfPendingMigrationsProbe` (`GetPendingMigrationsAsync` guarded by a `to_regclass` history-table check); seed waits quietly with a bounded timeout; regression and probe tests.
**Effort:** 2 (by-diff budget), single orchestrator pass
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
