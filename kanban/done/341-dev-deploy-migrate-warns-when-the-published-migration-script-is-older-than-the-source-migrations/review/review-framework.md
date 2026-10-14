# Review framework

## Budget (by-diff)

- Lines changed: 372
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 341

**Date:** 2026-10-15
**Host task:** kanban/to-do/341-dev-deploy-migrate-warns-when-the-published-migration-script-is-older-than-the-source-migrations/
**Diff scope:** branch `task/341-dev-deploy-migrate-warns-when-the-published-migrat` vs `master` (commit 0ea0069a1)
**Plan / brief:** task.md Requirements — stale-script check in `dev deploy migrate`, `--allow-stale`, report qualifier, aca handling, tests, tw-deploy note
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
