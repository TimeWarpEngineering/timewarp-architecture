# Review framework

## Budget (by-diff)

- Lines changed: 187
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 362

**Date:** 2026-10-14
**Host task:** kanban/to-do/362-bump-net-11-rc1-to-rc2/
**Diff scope:** branch `task/362-bump-net-11-rc1-to-rc2` vs `origin/master` (commit 9d56a766e)
**Plan / brief:** move SDK and shared-framework package pins from .NET 11 RC1 to RC2; EF Core held on RC1 for Npgsql
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
