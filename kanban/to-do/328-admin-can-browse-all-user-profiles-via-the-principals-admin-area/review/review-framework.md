# Review framework

## Budget (by-diff)

- Lines changed: 2668
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 328

**Date:** 2026-10-13
**Host task:** kanban/to-do/328-admin-can-browse-all-user-profiles-via-the-principals-admin-area/
**Diff scope:** branch `task/328-admin-can-browse-all-user-profiles-via-the-princip` vs `master` (commits a506514d9, dfccd1e8a)
**Plan / brief:** task.md Decision log + Requirements; implementation summary in task.md Results
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
