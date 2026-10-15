# Review framework

## Budget (by-diff)

- Lines changed: 475
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 366

**Date:** 2026-10-15
**Host task:** kanban/to-do/366-split-the-ci-job-into-parallel-jobs-for-build-and-fast-tests-playwright-integration-and-publish-checks/
**Diff scope:** branch task/366 vs master (commit 895f03820)
**Plan / brief:** split the single `ci` job into parallel `dev workflow --phase/--group` matrix jobs, plus a `ci` roll-up and a single `release` job; convention-derived TestGroups with a guard test.
**Effort:** 2 (by-diff budget), roster: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
